using Codice.Client.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VE2.Common;
using VE2.NonCore.Instancing.VComponents.NonInteractableInterfaces;
using VE2.Core.VComponents.InternalInterfaces;
using static VE2.Common.CommonSerializables;

namespace VE2.NonCore.Instancing.VComponents.Internal
{

    public class RigidbodySyncableService
    {
        #region Interfaces
        public IRigidbodySyncableStateModule StateModule => _stateModule;
        IGrabbableRigidbody _grabbableRigidbody;
        #endregion

        #region Modules
        private readonly RigidbodySyncableStateModule _stateModule;
        private readonly RigidbodySyncableStateConfig _config;
        #endregion

        private IRigidbodyWrapper _rigidbody;
        private bool _isKinematicOnStart;
        private List<RigidbodySyncableState> _receivedRigidbodyStates;

        private float _timeDifferenceFromHost;

        private readonly float _timeBehind = 0.04f;
        private float _localFixedTime = 0f;
        private float _localRealTime = 0f;
        private readonly float _fakePing = 0.0f;

        private bool _isGrabbed = false;

        public RigidbodySyncableService(RigidbodySyncableStateConfig config, VE2Serializable state, string id, WorldStateModulesContainer worldStateModulesContainer, RigidbodyWrapper rigidbodyWrapper)
        {
            _config = config;
            _stateModule = new(state, config, id, worldStateModulesContainer);
            _rigidbody = rigidbodyWrapper;
            _isKinematicOnStart = _rigidbody.isKinematic;

            SetupNonHostRigidbody();

            _receivedRigidbodyStates = new();

            _stateModule.OnReceiveState?.AddListener(HandleReceiveRigidbodyState);
            _stateModule.OnHostChanged += HandleHostChanged;
        }

        public RigidbodySyncableService(RigidbodySyncableStateConfig config, VE2Serializable state, string id, WorldStateModulesContainer worldStateModulesContainer, RigidbodyWrapper rigidbodyWrapper, IGrabbableRigidbody grabbableRigidbody)
            : this(config, state, id, worldStateModulesContainer, rigidbodyWrapper)
        {
            _grabbableRigidbody = grabbableRigidbody;
            _grabbableRigidbody.InternalOnGrab += HandleOnGrab;
            _grabbableRigidbody.InternalOnDrop += HandleOnDrop;
        }

        private void HandleOnGrab(ushort grabberClientID)
        {
            _isGrabbed = true;
        }

        private void HandleOnDrop(ushort grabberClientID)
        {
            _isGrabbed = false;
        }

        public void HandleFixedUpdate(float fixedTime)
        {
            _stateModule.HandleFixedUpdate();

            _localFixedTime = fixedTime;

            if (_stateModule.IsHost && !_isGrabbed)
            {
                if (_config.LogDebugMessages)
                {
                    Debug.Log($"Setting state fixed time {fixedTime}, position {_rigidbody.position}, rotation {_rigidbody.rotation}");
                }
                _stateModule.SetState(fixedTime, _rigidbody.position, _rigidbody.rotation);
            }

        }

        public void HandleUpdate(float timeSinceStartup)
        {
            _localRealTime = timeSinceStartup;

            if (!_stateModule.IsHost && !_isGrabbed)
            {
                InterpolateRigidbody();
            }

        }

        public void TearDown()
        {
            _stateModule.OnReceiveState?.RemoveListener(HandleReceiveRigidbodyState);
            _stateModule.OnHostChanged -= HandleHostChanged;
            _stateModule.TearDown();
        }

        private void HandleHostChanged(ushort newHostID)
        {
            // Find out who the new host is!
            if (_stateModule.IsHost)
            {   
                if (_receivedRigidbodyStates.Count >= 2)
                {
                    // Calculate RB velocities, which we take to be the most recent we received for now
                    (RigidbodySyncableState previous, RigidbodySyncableState next) latestStates = GetRelevantRigidbodyStates(_receivedRigidbodyStates.Count - 1);
                    Vector3 linearVelocity = GetVelocityFromStates(latestStates);
                    Vector3 angularVelocity = GetAngularVelocityFromStates(latestStates);

                    _rigidbody.isKinematic = _isKinematicOnStart;
                    SetRigidbodyValues(latestStates.next.Position, latestStates.next.Rotation, linearVelocity, angularVelocity);
                }
                else if (_receivedRigidbodyStates.Count == 1)
                {
                    // If we have fewer states, we have yet to receive many states and can't assume a velocity
                    // Revert isKinematic
                    _rigidbody.isKinematic = _isKinematicOnStart;
                    SetRigidbodyValues(_receivedRigidbodyStates[0].Position, _receivedRigidbodyStates[0].Rotation);
                }
                else
                {
                    _rigidbody.isKinematic = _isKinematicOnStart;
                }
            }

            _receivedRigidbodyStates.Clear();
        }

        #region Receive States Logic
        public void HandleReceiveRigidbodyState(float fixedTime, Vector3 position, Quaternion rotation)
        {
            if (!_stateModule.IsHost)
            {
                if (_config.LogDebugMessages)
                {
                    Debug.Log($"Received state fixed time {fixedTime}, position {position}, rotation {rotation}");
                }

                if (_receivedRigidbodyStates.Count == 0)
                {
                    _timeDifferenceFromHost = fixedTime - _localRealTime;
                }
                    
                AddReceivedStateToHistory(new(fixedTime, position, rotation));
            }
        }

        private void AddReceivedStateToHistory(RigidbodySyncableState newState)
        {
            int index = _receivedRigidbodyStates.FindIndex(rbState => rbState.FixedTime > newState.FixedTime);

            if (index == -1)
            {
                // If no state has a FixedTime > NewFixedTime, add to the end
                _receivedRigidbodyStates.Add(newState);
            }
            else
            {
                // Insert before the first state with FixedTime > NewFixedTime
                _receivedRigidbodyStates.Insert(index, newState);
            }
        }
        #endregion


        #region Interpolation Logic

        private void InterpolateRigidbody()
        {
            int numStates = _receivedRigidbodyStates.Count;

            if (numStates == 1)
            {
                SetRigidbodyValues(_receivedRigidbodyStates[0]);
            }
            else if (numStates >= 2)
            {
                // Calculate the time the rigidbody should be displayed at
                float delayedLocalTime = _localRealTime + _timeDifferenceFromHost - _timeBehind - _fakePing;
                
                // Calculate index of next state
                int index = _receivedRigidbodyStates.FindIndex(rbState => rbState.FixedTime > delayedLocalTime);

                // Get the previous and next states to interpolate betwen
                (RigidbodySyncableState previousState, RigidbodySyncableState nextState) = GetRelevantRigidbodyStates(index);

                ClearHistoricalStates(index);

                // Calculate interpolation parameter
                float lerpParameter = Mathf.InverseLerp(previousState.FixedTime, nextState.FixedTime, delayedLocalTime);

                // Do the interpolation
                SetRigidbodyValues(Vector3.Lerp(previousState.Position, nextState.Position, lerpParameter),
                    Quaternion.Slerp(previousState.Rotation, nextState.Rotation, lerpParameter));
            }
        }

        private (RigidbodySyncableState previous, RigidbodySyncableState next) GetRelevantRigidbodyStates(int indexOfNextState)
        {
            RigidbodySyncableState previousState;
            RigidbodySyncableState nextState;

            switch (indexOfNextState)
            {
                case -1:
                    // In this case we're ahead and need to extrapolate from the last two
                    previousState = _receivedRigidbodyStates[^2];
                    nextState = _receivedRigidbodyStates[^1];
                    break;
                case 0:
                    // In this case we're behind all states and need to extrapolate behind the first two states
                    previousState = _receivedRigidbodyStates[0];
                    nextState = _receivedRigidbodyStates[1];
                    break;
                default:
                    // In the typical case we have two states that we can interpolate between
                    previousState = _receivedRigidbodyStates[indexOfNextState - 1];
                    nextState = _receivedRigidbodyStates[indexOfNextState];
                    break;
            }
            return (previousState, nextState);
        }

        private void ClearHistoricalStates(int indexOfNextState)
        {
            if (indexOfNextState == -1)
                _receivedRigidbodyStates.RemoveRange(0, _receivedRigidbodyStates.Count - 2);
            else if (indexOfNextState >= 1)
                _receivedRigidbodyStates.RemoveRange(0, indexOfNextState - 1);
            // If index == 0 there are no historical states, so we don't clear them
        }

        private Vector3 GetVelocityFromStates((RigidbodySyncableState previous, RigidbodySyncableState next) statesTuple)
        {
            // Calculate the time difference
            float deltaTime = statesTuple.next.FixedTime - statesTuple.previous.FixedTime;

            if (Mathf.Approximately(deltaTime, 0f))
                return Vector3.zero;

            return (statesTuple.next.Position - statesTuple.previous.Position) / deltaTime;
        }

        private Vector3 GetAngularVelocityFromStates((RigidbodySyncableState previous, RigidbodySyncableState next) statesTuple)
        {
            // Calculate the time difference
            float deltaTime = statesTuple.next.FixedTime - statesTuple.previous.FixedTime;

            if (Mathf.Approximately(deltaTime, 0f))
                return Vector3.zero;

            // Calculate the relative rotation
            Quaternion deltaRotation = statesTuple.next.Rotation * Quaternion.Inverse(statesTuple.previous.Rotation);

            // Extract the angle and axis from the delta rotation
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

            // Ensure the angle is in radians for velocity calculation
            angle *= Mathf.Deg2Rad;

            // Calculate the angular velocity vector
            Vector3 angularVelocity = (axis * angle) / deltaTime;

            return angularVelocity;
        }

        private void SetRigidbodyValues(RigidbodySyncableState newState)
        {
            _rigidbody.position = newState.Position;
            _rigidbody.rotation = newState.Rotation;
        }

        private void SetRigidbodyValues(Vector3 position, Quaternion rotation)
        {
            _rigidbody.position = position;
            _rigidbody.rotation = rotation;
        }

        private void SetRigidbodyValues(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
        {
            _rigidbody.position = position;
            _rigidbody.rotation = rotation;
            _rigidbody.linearVelocity = velocity;
            _rigidbody.angularVelocity = angularVelocity;
        }


        #endregion

        private void LogStates()
        {
            string stateString = "rbState times = [";
            foreach (RigidbodySyncableState rbState in _receivedRigidbodyStates)
            {
                stateString += rbState.FixedTime.ToString() + " ";
            }

            stateString += "]";
            Debug.Log(stateString);
        }

        private void SetupNonHostRigidbody()
        {
            if (!_stateModule.IsHost)
            {
                _rigidbody.isKinematic = true;
            }
        }
    }
}
