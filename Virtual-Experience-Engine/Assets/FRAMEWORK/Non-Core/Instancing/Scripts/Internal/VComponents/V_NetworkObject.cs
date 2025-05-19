using System;
using UnityEngine;
using VE2.Common.API;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;

namespace VE2.NonCore.Instancing.Internal
{
    internal class V_NetworkObject : MonoBehaviour, IV_NetworkObject
    {
        [SerializeField, HideLabel, IgnoreParent] private NetworkObjectStateConfig _config = new();
        [SerializeField, HideInInspector] private NetworkObjectState _state = new();

        #region Plugin Interfaces
        INetworkObjectStateModule IV_NetworkObject._StateModule => _Service.StateModule;
        #endregion

        private NetworkObjectService _service = null;
        private NetworkObjectService _Service
        {
            get
            {
                if (_service == null)
                    OnEnable();
                return _service;
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || _service != null)
                return;

            string id = "NetObj-" + gameObject.name;

            if (VE2API.InstanceService == null)
            {
                Debug.LogError("Instance service is null, cannot initialise NetworkObject, please add a V_InstanceIntegration component to the scene.");
                return;
            }

            _service = new NetworkObjectService(_config, _state, id, VE2API.WorldStateSyncableContainer);
        }

        private void FixedUpdate()
        {
            _service?.HandleFixedUpdate();
        }

        private void OnDisable()
        {
            _service?.TearDown();
            _service = null;
        }
    }
}