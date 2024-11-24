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
        private List<(uint, RigidbodySyncableState)> _rbStates;

        public RigidbodySyncableService(RigidbodySyncableStateConfig config, VE2Serializable state, string id, WorldStateModulesContainer worldStateModulesContainer, RigidbodyWrapper rigidbodyWrapper)
        {
            Debug.Log("Creating Rigidbody Syncable Service");

            _StateModule = new(state, config, id, worldStateModulesContainer);
            _rigidbody = rigidbodyWrapper;
            _isKinematicOnStart = _rigidbody.isKinematic;

            Debug.Log($"StateModule {_StateModule}, rb {_rigidbody}, kinematicOnStart {_isKinematicOnStart}, multiplayerSupportPresent {_StateModule.MultiplayerSupportPresent}");

            if (!_StateModule.IsHost)
            {
                _rigidbody.isKinematic = true;
            }

            Debug.Log($"isHost {_StateModule.IsHost}");

            _rbStates = new(); // TODO: store received states in a list, including a pseudo arrival-time, for interpolation by non-host

            _StateModule.OnReceiveState.AddListener(HandleReceiveRigidbodyState);
        }

        public void HandleFixedUpdate()
        {
            _StateModule.HandleFixedUpdate();

            if (_StateModule.IsHost)
            {
                _StateModule.SetState(_rigidbody.position, _rigidbody.rotation);
            }
        }

        public void TearDown()
        {
            _StateModule.TearDown();
        }

        public void HandleReceiveRigidbodyState(Vector3 Position, Quaternion Rotation)
        {
            if (_StateModule.IsHost)
            {
                Debug.Log($"Received Rigidbody state as the host against all odds! Position: {Position}, Rotation {Rotation}");
            }
            else
            {
                _rigidbody.position = Position;
                _rigidbody.rotation = Rotation;

                Debug.Log($"Received Rigidbody state from the host against all odds! Position: {Position}, Rotation {Rotation}");
            }
                
        }
    }
}
