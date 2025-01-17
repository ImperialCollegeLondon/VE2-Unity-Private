using System;
using System.Collections;
using UnityEngine;
using VE2.Common;
using VE2.NonCore.Instancing.VComponents.Internal;
using VE2.NonCore.Instancing.VComponents.NonInteractableInterfaces;
using VE2.NonCore.Instancing.VComponents.PluginInterfaces;
using VE2.Core.VComponents.InternalInterfaces;

namespace VE2.NonCore.Instancing.VComponents.MonoBehaviours
{
    internal class V_RigidbodySyncable : MonoBehaviour, IV_RigidbodySyncable
    {
        [SerializeField, HideLabel, IgnoreParent] private RigidbodySyncableStateConfig _config = new();
        [SerializeField, HideInInspector] private RigidbodySyncableState _state = new();

        #region Plugin Interfaces
        IRigidbodySyncableStateModule IV_RigidbodySyncable._StateModule => _service.StateModule;
        #endregion

        private RigidbodySyncableService _service = null;
        private RigidbodyWrapper _rigidbodyWrapper = null;

        private void OnEnable()
        {
            if (_config.MultiplayerSupport.IsConnectedToServer)
            {
                CreateService();
            }
            else
            {
                _config.MultiplayerSupport.OnConnectedToInstance += CreateService;
            }

            // StartCoroutine(nameof(DelayedStart));
        }

        private void CreateService()
        {
            _config.MultiplayerSupport.OnConnectedToInstance -= CreateService;

            string id = "RBS-" + gameObject.name;
            _rigidbodyWrapper = new(GetComponent<Rigidbody>());

            if (TryGetComponent(out IGrabbableRigidbody grabbableRigidbody))
            {
                _service = new RigidbodySyncableService(_config, _state, id, VE2CoreServiceLocator.Instance.WorldStateModulesContainer, _rigidbodyWrapper, grabbableRigidbody);
            }
            else
            {
                _service = new RigidbodySyncableService(_config, _state, id, VE2CoreServiceLocator.Instance.WorldStateModulesContainer, _rigidbodyWrapper);
            }

        }

        private void FixedUpdate()
        {
            _service?.HandleFixedUpdate(Time.fixedTime);
        }

        private void Update()
        {
            _service?.HandleUpdate(Time.realtimeSinceStartup);
        }

        private void OnDisable()
        {
            _service.TearDown();
            _service = null;
        }
    }
}
