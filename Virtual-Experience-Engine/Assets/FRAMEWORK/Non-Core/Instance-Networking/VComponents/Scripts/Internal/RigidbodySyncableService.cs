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
        public IRigidbodySyncableStateModule StateModule => _StateModule;
        #endregion

        #region Modules
        private readonly RigidbodySyncableStateModule _StateModule;
        #endregion

        private IRigidbodyWrapper _rigidbody;
        private bool _isKinematicOnStart;
        private List<RigidbodySyncableState> _rbStates;

        private float _timeDifferenceFromHost;

        private readonly float _timeBehind = 0.04f;
        private float _localFixedTimeAtUpdate = 0f;

        public RigidbodySyncableService(RigidbodySyncableStateConfig config, VE2Serializable state, string id, WorldStateModulesContainer worldStateModulesContainer, RigidbodyWrapper rigidbodyWrapper)
        {

            _StateModule = new(state, config, id, worldStateModulesContainer);
            _rigidbody = rigidbodyWrapper;
            _isKinematicOnStart = _rigidbody.isKinematic;

            SetNonHostKinematic();

            _rbStates = new(); // TODO: store received states in a list, including a pseudo arrival-time, for interpolation by non-host

            _StateModule.OnReceiveState?.AddListener(HandleReceiveRigidbodyState);
        }

        public void HandleFixedUpdate(float fixedTime)
        {
            _StateModule.HandleFixedUpdate();

            _localFixedTimeAtUpdate = fixedTime;

            // Temporary test of host syncing without interpolation
            if (_StateModule.IsHost)
            {
                _StateModule.SetState(fixedTime, _rigidbody.position, _rigidbody.rotation);
            }
            else
            {
                InterpolateRigidbody();
            }
        }

        public void TearDown()
        {
            _StateModule.TearDown();
        }

        public void HandleReceiveRigidbodyState(float FixedTime, Vector3 Position, Quaternion Rotation)
        {
            // Temporary test of host syncing without interpolation
            if (_StateModule.IsHost)
            {

            }
            else
            {
                if (_rbStates.Count == 0)
                {
                    _timeDifferenceFromHost = FixedTime - _localFixedTimeAtUpdate;
                }
                    
                AddReceivedStateToHistory(new(FixedTime, Position, Rotation));
            }
        }

        private void SetNonHostKinematic()
        {
            if (!_StateModule.IsHost)
            {
                _rigidbody.isKinematic = true;
            }
        }

        private void AddReceivedStateToHistory(RigidbodySyncableState newState)
        {
            int index = _rbStates.FindIndex(rbState => rbState.FixedTime > newState.FixedTime);

            if (index == -1)
            {
                // If no state has a FixedTime > NewFixedTime, add to the end
                _rbStates.Add(newState);
            }
            else
            {
                // Insert before the first state with FixedTime > NewFixedTime
                _rbStates.Insert(index, newState);
            }
        }

        private void InterpolateRigidbody()
        {
            if (_rbStates.Count == 1)
            {
                SetRigidbodyValues(_rbStates[0]);
            }
            else if (_rbStates.Count >= 2)
            {
                // Find where we sit in the _rbStates arrangement
                float delayedLocalTime = _localFixedTimeAtUpdate + _timeDifferenceFromHost - _timeBehind;
                int index = _rbStates.FindIndex(rbState => rbState.FixedTime > delayedLocalTime);

                RigidbodySyncableState previousState;
                RigidbodySyncableState nextState;

                if (index == -1 || index == _rbStates.Count-1)
                {
                    // In this case we're ahead and need to extrapolate from the last two
                    previousState = _rbStates[^2];
                    nextState = _rbStates[^1];

                    // Throw away old _rbStates that we're beyond now
                    _rbStates.RemoveRange(0, _rbStates.Count - 2);
                }
                else
                {
                    previousState = _rbStates[index];
                    nextState = _rbStates[index + 1];

                    // Throw away old _rbStates that we're beyond now
                    _rbStates.RemoveRange(0, index);
                }

                

                // Calculate interpolation parameter
                float lerpParameter = InverseLerp(delayedLocalTime, previousState.FixedTime, nextState.FixedTime);

                // Do the interpolation
                SetRigidbodyValues(Vector3.Lerp(previousState.Position, nextState.Position, lerpParameter), Quaternion.Lerp(previousState.Rotation, nextState.Rotation, lerpParameter));
            }
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

        private float InverseLerp(float x, float a, float b)
        {
            if (b - a != 0)
            {
                return (x - a) / (b - a);
            }
            else
            {
                return 0;
            }
            
        }
    }
}
