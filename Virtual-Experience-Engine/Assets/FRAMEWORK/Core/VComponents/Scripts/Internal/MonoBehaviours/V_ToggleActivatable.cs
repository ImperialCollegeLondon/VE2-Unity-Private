using System;
using UnityEngine;
using VE2.Common;
using VE2.Core.VComponents.NonInteractableInterfaces;
using VE2.Core.VComponents.InteractableFindables;
using VE2.Core.VComponents.PluginInterfaces;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.Internal;

namespace VE2.Core.VComponents.Integration
{
    internal class V_ToggleActivatable : VComponentBase, IV_ToggleActivatable, IRangedPlayerInteractableIntegrator, ICollidePlayerInteractableIntegrator
    {
        [SerializeField, HideLabel, IgnoreParent] private ToggleActivatableConfig _config = new(); 
        [SerializeField, HideInInspector] private SingleInteractorActivatableState _state = new();

        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule IV_ToggleActivatable._StateModule => _service.StateModule;
        IRangedClickInteractionModule IV_ToggleActivatable._RangedClickModule => _service.RangedClickInteractionModule;
        #endregion

        #region Player Interfaces
        ICollideInteractionModule ICollidePlayerInteractableIntegrator.CollideInteractionModule => _service.ColliderInteractionModule;
        IRangedInteractionModule IRangedPlayerInteractableIntegrator.RangedInteractionModule => _service.RangedClickInteractionModule;
        #endregion
        
        private ToggleActivatableService _service = null;

        private void OnEnable()
        {
            string id = "Activatable-" + gameObject.name; 
            _service = new ToggleActivatableService(_config, _state, id, VComponents_Locator.Instance.WorldStateModulesContainer);
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