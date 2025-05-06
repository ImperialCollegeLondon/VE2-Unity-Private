using System;
using System.Collections;
using UnityEngine;
using VE2.Common.API;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;

namespace VE2.NonCore.Instancing.Internal
{
    internal class V_RigidbodySyncable : MonoBehaviour, IV_RigidbodySyncable
    {
        [SerializeField, HideLabel, IgnoreParent] private RigidbodySyncableStateConfig _config = new();
        [SerializeField, HideInInspector] private RigidbodySyncableState _state = new();

        #region Plugin Interfaces
        IRigidbodySyncableStateModule IV_RigidbodySyncable._StateModule => _service.StateModule;
        #endregion

        private RigidbodySyncableService _service = null;

        private void OnEnable()
        {
            string id = "RBS-" + gameObject.name;
            IRigidbodyWrapper rigidbodyWrapper = new RigidbodyWrapper(GetComponent<Rigidbody>());
            IGrabbableRigidbody grabbableRigidbody = GetComponent<IGrabbableRigidbody>();

            if (VE2API.InstanceService == null)
            {
                Debug.LogError("Instance service is null, cannot initialise RigidbodySyncable, please add a V_InstanceIntegration component to the scene.");
                return;
            }
            
            _service = new RigidbodySyncableService(_config, _state, id, VE2API.WorldStateSyncableContainer, VE2API.InstanceService, rigidbodyWrapper, grabbableRigidbody);
        }


        private void FixedUpdate()
        {
            _service?.HandleFixedUpdate();
        }

        private void Update()
        {
            _service?.HandleUpdate();
        }

        private void OnDisable()
        {
            _service?.TearDown();
            _service = null;
        }
    }
}
