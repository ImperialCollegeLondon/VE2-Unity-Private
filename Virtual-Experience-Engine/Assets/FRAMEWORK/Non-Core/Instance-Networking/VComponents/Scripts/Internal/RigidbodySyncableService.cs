using Codice.Client.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common;
using VE2.NonCore.Instancing.VComponents.NonInteractableInterfaces;
using static VE2.Common.CommonSerializables;

namespace VE2.NonCore.Instancing.VComponents.Internal
{

    public class RigidbodySyncableService
    {
        #region Interfaces
        public IRigidbodySyncableStateModule StateModule => _stateModule;
        #endregion

        #region Modules
        private readonly RigidbodySyncableStateModule _stateModule;
        #endregion

        private IRigidbodyWrapper _rigidbody;
        private bool _isKinematicOnStart;
        private List<RigidbodySyncableState> _receivedRigidbodyStates;

        private float _timeDifferenceFromHost;

        private readonly float _timeBehind = 0.04f;
        private float _localFixedTime = 0f;
        private float _localRealTime = 0f;
        private readonly float _fakePing = 0.0f;

        public RigidbodySyncableService(RigidbodySyncableStateConfig config, VE2Serializable state, string id, WorldStateModulesContainer worldStateModulesContainer, RigidbodyWrapper rigidbodyWrapper)
        {

            _stateModule = new(state, config, id, worldStateModulesContainer);
            _rigidbody = rigidbodyWrapper;
            _isKinematicOnStart = _rigidbody.isKinematic;

            SetupNonHostRigidbody();

            _receivedRigidbodyStates = new();

            _stateModule.OnReceiveState?.AddListener(HandleReceiveRigidbodyState);
        }

        public void HandleFixedUpdate(float fixedTime)
        {
            _stateModule.HandleFixedUpdate();

            _localFixedTime = fixedTime;

            if (_stateModule.IsHost)
            {
                _stateModule.SetState(fixedTime, _rigidbody.position, _rigidbody.rotation);
            }

        }

        public void HandleUpdate(float timeSinceStartup)
        {
            _localRealTime = timeSinceStartup;

            if (!_stateModule.IsHost)
            {
                InterpolateRigidbody();
            }

        }

        public void TearDown()
        {
            _stateModule.TearDown();
        }

        #region Receive States Logic
        public void HandleReceiveRigidbodyState(float FixedTime, Vector3 Position, Quaternion Rotation)
        {
            if (!_stateModule.IsHost)
            {
                if (_receivedRigidbodyStates.Count == 0)
                {
                    _timeDifferenceFromHost = FixedTime - _localRealTime;
                }
                    
                AddReceivedStateToHistory(new(FixedTime, Position, Rotation));
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
                (RigidbodySyncableState previous, RigidbodySyncableState next) interpolationStates = GetRelevantRigidbodyStates(index);

                ClearHistoricalStates(index);

                // Calculate interpolation parameter
                float lerpParameter = Mathf.InverseLerp(interpolationStates.previous.FixedTime, interpolationStates.next.FixedTime, delayedLocalTime);

                // Do the interpolation
                SetRigidbodyValues(Vector3.Lerp(interpolationStates.previous.Position, interpolationStates.next.Position, lerpParameter),
                    Quaternion.Slerp(interpolationStates.previous.Rotation, interpolationStates.next.Rotation, lerpParameter));
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
            return (statesTuple.next.Position + statesTuple.previous.Position) / 2;
        }

        private Quaternion GetAngularVelocityFromStates((RigidbodySyncableState previous, RigidbodySyncableState next) statesTuple)
        {
            return Quaternion.Slerp(statesTuple.next.Rotation, statesTuple.previous.Rotation, 0.5f);
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
