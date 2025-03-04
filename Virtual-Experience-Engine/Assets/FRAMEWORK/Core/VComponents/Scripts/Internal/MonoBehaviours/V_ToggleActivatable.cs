using System;
using UnityEngine;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class V_ToggleActivatable : MonoBehaviour, IV_ToggleActivatable, IRangedInteractionModuleProvider, ICollideInteractionModuleProvider
    {
        [SerializeField, HideLabel, IgnoreParent] private ToggleActivatableConfig _config = new(); 
        [SerializeField, HideInInspector] private SingleInteractorActivatableState _state = new();

        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule IV_ToggleActivatable._StateModule => _service.StateModule;
        IRangedClickInteractionModule IV_ToggleActivatable._RangedClickModule => _service.RangedClickInteractionModule;
        #endregion

        #region Player Interfaces
        ICollideInteractionModule ICollideInteractionModuleProvider.CollideInteractionModule => _service.ColliderInteractionModule;
        IRangedInteractionModule IRangedInteractionModuleProvider.RangedInteractionModule => _service.RangedClickInteractionModule;
        #endregion
        
        private ToggleActivatableService _service = null;

        private void OnEnable()
        {
            string id = "Activatable-" + gameObject.name; 
            _service = new ToggleActivatableService(_config, _state, id, VComponentsAPI.WorldStateSyncService);
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