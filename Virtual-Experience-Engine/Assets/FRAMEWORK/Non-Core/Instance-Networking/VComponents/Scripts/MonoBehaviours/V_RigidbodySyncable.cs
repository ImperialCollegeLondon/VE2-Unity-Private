using System;
using UnityEngine;
using VE2.Common;
using VE2.NonCore.Instancing.VComponents.Internal;
using VE2.NonCore.Instancing.VComponents.NonInteractableInterfaces;
using VE2.NonCore.Instancing.VComponents.PluginInterfaces;

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
            string id = "RBS-" + gameObject.name;
            _rigidbodyWrapper = new(GetComponent<Rigidbody>());
            _service = new RigidbodySyncableService(_config, _state, id, VE2CoreServiceLocator.Instance.WorldStateModulesContainer, _rigidbodyWrapper);
            Debug.Log($"Hit OnEnable and created service");
        }

        private void FixedUpdate()
        {
            _service.HandleFixedUpdate();
        }

        private void OnDisable()
        {
            _service.TearDown();
            _service = null;
        }
    }
}
