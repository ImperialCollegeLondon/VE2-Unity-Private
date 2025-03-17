using Codice.Client.Common;
using log4net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;
using static VE2.Core.Common.CommonSerializables;
using Time = UnityEngine.Time;

namespace VE2.NonCore.Instancing.Internal
{
    internal class RigidbodySyncableService
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
        private IInstanceService _instanceService;
        private bool _isKinematicOnStart;
        private List<RigidbodySyncableState> _receivedRigidbodyStates;

        private float _timeDifferenceFromHost;

        private readonly float _timeBehind = 0.04f;
        private readonly float _lagCompensationAdditionalTime = 0.4f;

        private bool _hostNotSendingStates = false;
        private bool _nonHostSimulating = false;
        private ushort? _currentGrabberID;

        private uint _grabID = 0;

        public RigidbodySyncableService(RigidbodySyncableStateConfig config, VE2Serializable state, string id, IWorldStateSyncService worldStateSyncService, IInstanceService instanceService, IRigidbodyWrapper rigidbodyWrapper, IGrabbableRigidbody grabbableRigidbody)
        {
            _config = config;
            _stateModule = new(state, config, id, worldStateSyncService, instanceService);
            _instanceService = instanceService;
            _rigidbody = rigidbodyWrapper;
            _isKinematicOnStart = _rigidbody.isKinematic;

            if (grabbableRigidbody != null)
            {
                _grabbableRigidbody = grabbableRigidbody;
                _grabbableRigidbody.InternalOnGrab += HandleOnGrab;
                _grabbableRigidbody.InternalOnDrop += HandleOnDrop;
            }

            _receivedRigidbodyStates = new();

            _stateModule.OnReceiveState?.AddListener(HandleReceiveRigidbodyState);
            _stateModule.OnHostChanged += HandleHostChanged;

            grabbableRigidbody.FreeGrabbableHandlesKinematics = false;

        }

        private void HandleOnGrab(ushort grabberClientID)
        {
            _grabID++;
            _currentGrabberID = grabberClientID;

            if (_stateModule.IsHost)
            {
                // Host stops sending states, store kinematic state
                _hostNotSendingStates = true;
                _isKinematicOnStart = _rigidbody.isKinematic;
            }
            else
            {
                // Non host starts simulating for themselves, removes old states
                _nonHostSimulating = true;
                _receivedRigidbodyStates.Clear();
            }

            _rigidbody.isKinematic = false;
        }

        private void HandleOnDrop(ushort grabberClientID)
        {
            _currentGrabberID = null;

            if (_instanceService.LocalClientID == grabberClientID)
            {
                if (_stateModule.IsHost)
                {
                    // Host who dropped immediately starts sending messages again
                    _hostNotSendingStates = false;
                }
                else
                {
                    // Non host dropper sends state to host
                    _stateModule.SetStateFromNonHost(Time.fixedTime, _rigidbody.position, _rigidbody.rotation, _grabID, _instanceService.Ping, _rigidbody.linearVelocity, _rigidbody.angularVelocity);
                }
            }
        }

        private void PerformLagCompensationForDroppedGrabbable(float roundTripTimeNonHost)
        {

            // Make all rigidbodys in the scene, apart from this one, kinematic
            Dictionary<IRigidbodyWrapper, bool> kinematicStates = new();

            foreach (Rigidbody rigidbodyInScene in GameObject.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                if (_rigidbody.Equals(rigidbodyInScene))
                {
                    continue;
                }

                IRigidbodyWrapper rigidbodyInSceneWrapper = new RigidbodyWrapper(rigidbodyInScene);
                kinematicStates.Add(rigidbodyInSceneWrapper, rigidbodyInSceneWrapper.isKinematic);
                rigidbodyInSceneWrapper.isKinematic = true;
            }

            float lagCompensationTime = _timeBehind + (roundTripTimeNonHost + _lagCompensationAdditionalTime)/1000f;
            int cyclesToSimulate = Mathf.CeilToInt(lagCompensationTime / Time.fixedDeltaTime) + 1;

            // Simulate physics in full steps
            Physics.simulationMode = SimulationMode.Script;

            for (int i = 0; i < cyclesToSimulate; i++)
                Physics.Simulate(UnityEngine.Time.fixedDeltaTime);

            Physics.simulationMode = SimulationMode.FixedUpdate;

            foreach (KeyValuePair<IRigidbodyWrapper, bool> kinematicState in kinematicStates) //Unlock the RBs that we just locked
                kinematicState.Key.isKinematic = kinematicState.Value;
        }

        public void HandleFixedUpdate()
        {
            _stateModule.HandleFixedUpdate();

            // Hosts send states on FixedUpdate when hostNotSendingStates flag is false
            if (_stateModule.IsHost && !_hostNotSendingStates)
            {
                if (_config.LogSendReceiveDebugMessages)
                { 
                    Debug.Log($"Setting state fixed time {Time.fixedTime}, position {_rigidbody.position}, rotation {_rigidbody.rotation}, grabID {_grabID}");
                }
                _stateModule.SetStateFromHost(Time.fixedTime, _rigidbody.position, _rigidbody.rotation, _grabID);
            }
        }

        public void HandleUpdate()
        {
            // Non host interpolates on Update when not simulating for themselves
            if (!_stateModule.IsHost && !_nonHostSimulating)
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
                _rigidbody.isKinematic = _isKinematicOnStart;

                // No longer non-host, return to default
                _nonHostSimulating = false;
                // If someone is grabbing, don't send states. Otherwise, start sending states
                _hostNotSendingStates = (_currentGrabberID != null);


                if (_receivedRigidbodyStates.Count >= 2)
                {
                    // Calculate RB velocities, which we take to be the most recent we received for now
                    (RigidbodySyncableState previous, RigidbodySyncableState next) latestStates = GetRelevantRigidbodyStates(_receivedRigidbodyStates.Count - 1);
                    Vector3 linearVelocity = GetVelocityFromStates(latestStates);
                    Vector3 angularVelocity = GetAngularVelocityFromStates(latestStates);

                    SetRigidbodyValues(latestStates.next.Position, latestStates.next.Rotation, linearVelocity, angularVelocity);
                }
                else if (_receivedRigidbodyStates.Count == 1)
                {
                    // If we have fewer states, we have yet to receive many states and can't assume a velocity
                    SetRigidbodyValues(_receivedRigidbodyStates[0].Position, _receivedRigidbodyStates[0].Rotation);
                }

            }

            _receivedRigidbodyStates.Clear();
        }

        #region Receive States Logic
        public void HandleReceiveRigidbodyState(RigidbodySyncableState receivedState)
        {
            if (_config.LogSendReceiveDebugMessages)
            {
                Debug.Log($"Received state from host {receivedState.FromHost}, fixed time {receivedState.FixedTime}, position {receivedState.Position}, rotation {receivedState.Rotation}, grabID {_grabID}");
            }

            if (_stateModule.IsHost)
            {
                // If a host receives a state, can we assume it's a drop state?
                _rigidbody.isKinematic = _isKinematicOnStart;
                SetRigidbodyValuesFromDrop(receivedState);
                PerformLagCompensationForDroppedGrabbable(receivedState.LatestRoundTripTime);
                _hostNotSendingStates = false;
            } 
            else if (!_stateModule.IsHost)
            {

                if (receivedState.GrabID != _grabID || !receivedState.FromHost)
                {
                    // Ignore all states from before the latest grab, or from another non host
                    return;
                }

                if (_receivedRigidbodyStates.Count == 0)
                {
                    // When we start receiving rigidbody states again, we stop doing our own simulation
                    _nonHostSimulating = false;
                    _isKinematicOnStart = _rigidbody.isKinematic;
                    _rigidbody.isKinematic = true;
                    _timeDifferenceFromHost = receivedState.FixedTime - Time.fixedTime;
                }

                AddReceivedStateToHistory(new(receivedState.FixedTime, receivedState.Position, receivedState.Rotation, receivedState.GrabID));
            }
        }

        private void AddReceivedStateToHistory(RigidbodySyncableState newState)
        {
            if (_config.DrawInterpolationLines)
            {
                // Draw lines for received states to compare to interpolation
                Debug.DrawLine(_rigidbody.position, _rigidbody.position + Vector3.Cross(_rigidbody.linearVelocity, Vector3.up).normalized / 5, Color.green, 20f);
            }

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
                // SetRigidbodyValues(_receivedRigidbodyStates[0]);
            }
            else if (numStates >= 2)
            { 
                // Calculate the time the rigidbody should be displayed at
                float delayedLocalTime = Time.time + _timeDifferenceFromHost - _timeBehind;
                
                // Calculate index of next state
                int index = _receivedRigidbodyStates.FindIndex(rbState => rbState.FixedTime > delayedLocalTime);

                // Get the previous and next states to interpolate betwen
                (RigidbodySyncableState previousState, RigidbodySyncableState nextState) = GetRelevantRigidbodyStates(index);

                ClearHistoricalStates(index);

                // Calculate interpolation parameter
                float lerpParameter = InverseLerpUnclamped(previousState.FixedTime, nextState.FixedTime, delayedLocalTime);

                // Do the interpolation
                SetRigidbodyValues(Vector3.Lerp(previousState.Position, nextState.Position, lerpParameter),
                    Quaternion.Slerp(previousState.Rotation, nextState.Rotation, lerpParameter));

                if (_config.DrawInterpolationLines)
                {
                    Color lineColour = lerpParameter >= 0 ? Color.white : Color.red;
                    Debug.DrawLine(_rigidbody.position, _rigidbody.position + Vector3.Cross(_rigidbody.linearVelocity, Vector3.up).normalized / 5, Color.white, 20f);
                }
                if (_config.LogInterpolationDebug) { 
                    Debug.Log($"LocalTime = {delayedLocalTime}, StateFixedTimes = {previousState.FixedTime} & {nextState.FixedTime}, lerpParam = {lerpParameter}");
                }
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

        private float InverseLerpUnclamped (float a, float b, float value)
        {
            if (a != b)
            {
                return (value - a) / (b - a);
            }

            return 0f;
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

        private void SetRigidbodyValuesFromDrop(RigidbodySyncableState newState)
        {
            _rigidbody.position = newState.Position;
            _rigidbody.rotation = newState.Rotation;
            _rigidbody.linearVelocity = newState.Velocity;
            _rigidbody.angularVelocity = newState.AngularVelocity;
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

    }
}
