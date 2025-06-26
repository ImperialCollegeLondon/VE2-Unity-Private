using System;
using System.Collections;
using UnityEngine;
using VE2.Common.API;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;

namespace VE2.NonCore.Instancing.Internal
{
    internal partial class V_RigidbodySyncable : IV_RigidbodySyncable
    {
        //No plugin-facing interfaces here (yet)
    }

    internal partial class V_RigidbodySyncable : BaseSyncableVComponent
    {
        [SerializeField, HideLabel, IgnoreParent] private RigidbodySyncableStateConfig _config = new();
        [SerializeField, HideInInspector] private RigidbodySyncableState _state = new();

        #region Plugin Interfaces
        IRigidbodySyncableStateModule _StateModule => _Service.StateModule;
        #endregion

        private RigidbodySyncableService _service = null;
        private RigidbodySyncableService _Service
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

            //string id = "RBS-" + gameObject.name;
            _idWrapper = new();
            _vComponentID = "RBS-";

            IRigidbodyWrapper rigidbodyWrapper = new RigidbodyWrapper(GetComponent<Rigidbody>());
            IGrabbableRigidbody grabbableRigidbody = GetComponent<IGrabbableRigidbody>();

            if (VE2API.InstanceService == null)
            {
                Debug.LogError("Instance service is null, cannot initialise RigidbodySyncable, please add a V_InstanceIntegration component to the scene.");
                return;
            }

            _service = new RigidbodySyncableService(_config, _state, _idWrapper, VE2API.WorldStateSyncableContainer, VE2API.InstanceService, rigidbodyWrapper, grabbableRigidbody);
        }


        protected override void FixedUpdate()
        {
            base.FixedUpdate();
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
