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

    internal partial class V_RigidbodySyncable : MonoBehaviour
    {
        public void OpenDocs() => Application.OpenURL("https://www.notion.so/V_RigidBodySyncable-20f0e4d8ed4d816fa5a6ebfb41761ffb?source=copy_link");
        [EditorButton(nameof(OpenDocs), "Open Docs", PositionType = ButtonPositionType.Above)]
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

            string id = "RBS-" + gameObject.name;
            IRigidbodyWrapper rigidbodyWrapper = new RigidbodyWrapper(GetComponent<Rigidbody>());
            IGrabbableRigidbody grabbableRigidbody = GetComponent<IGrabbableRigidbody>();

            if (VE2API.InstanceService == null)
            {
                //TODO, log this if logging level is set to verbose (once we actually have a logging system)
                //Debug.LogWarning("Instance service is null, cannot initialise RigidbodySyncable, please add a V_InstanceIntegration component to the scene.");
                return;
            }

            _service = new RigidbodySyncableService(_config, _state, id, VE2API.WorldStateSyncableContainer, VE2API.InstanceService as IInstanceServiceInternal, rigidbodyWrapper, grabbableRigidbody);
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
