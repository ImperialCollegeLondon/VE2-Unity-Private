using System;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.API;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;

namespace VE2.NonCore.Instancing.Internal
{
    internal partial class V_NetworkObject : IV_NetworkObject
    {
        #region State Module Interface
        //NOTE: Intended deviation from the pattern - see interface definition IV_NetworkObject
        INetworkObjectStateModule IV_NetworkObject._stateModule => _Service.StateModule;

        public UnityEvent<object> OnDataChange => _Service.StateModule.OnStateChange;
        public object CurrentData => _Service.StateModule.NetworkObject;
        #endregion
    }

    internal partial class V_NetworkObject : MonoBehaviour
    {
        [SerializeField, HideLabel, IgnoreParent] private NetworkObjectStateConfig _config = new();
        [SerializeField, HideInInspector] private NetworkObjectState _state = new();

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