using System;
using UnityEngine;
using VE2.Common;
using VE2.NonCore.Instancing.VComponents.Internal;
using VE2.NonCore.Instancing.VComponents.NonInteractableInterfaces;
using VE2.NonCore.Instancing.VComponents.PluginInterfaces;

namespace VE2.NonCore.Instancing.VComponents.MonoBehaviours
{
    internal class V_NetworkObject : MonoBehaviour, IV_NetworkObject
    {
        [SerializeField, HideLabel, IgnoreParent] private NetworkObjectStateConfig _config = new();
        [SerializeField, HideInInspector] private NetworkObjectState _state = new();

        #region Plugin Interfaces
        INetworkObjectStateModule IV_NetworkObject._StateModule => _service.StateModule;
        #endregion

        private NetworkObjectService _service = null;

        private void Reset()
        {
            //Kicks off the lazy init for the VCLocator instance
            var reference = VComponents_Locator.Instance;
        }

        private void OnEnable()
        {
            string id = "NetObj-" + gameObject.name;
            _service = new NetworkObjectService(_config, _state, id, VComponents_Locator.Instance.WorldStateModulesContainer);
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