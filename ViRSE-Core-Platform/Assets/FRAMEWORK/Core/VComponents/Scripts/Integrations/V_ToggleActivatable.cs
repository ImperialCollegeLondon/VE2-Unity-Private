using System;
using UnityEngine;
using ViRSE.Common;
using ViRSE.Core.VComponents.NonInteractableInterfaces;
using ViRSE.Core.VComponents.RaycastInterfaces;
using ViRSE.Core.VComponents.PluginInterfaces;
using VIRSE.Core.VComponents.InteractableInterfaces;
using ViRSE.Core.VComponents.Internal;

namespace ViRSE.Core.VComponents.Integration
{
    internal class V_ToggleActivatable : MonoBehaviour, IV_ToggleActivatable, IRangedClickPlayerInteractableIntegrator, ICollidePlayerInteractableIntegrator
    {
        [SerializeField, HideLabel, IgnoreParent] private ToggleActivatableConfig _config = new(); 
        [SerializeField, HideInInspector] private SingleInteractorActivatableState _state = new();

        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule IV_ToggleActivatable._StateModule => _toggleActivatable.StateModule;
        IRangedClickInteractionModule IV_ToggleActivatable._RangedClickModule => _toggleActivatable.RangedClickInteractionModule;
        #endregion

        #region Player Interfaces
        ICollideInteractionModule ICollidePlayerInteractableIntegrator._CollideInteractionModule => _toggleActivatable.ColliderInteractionModule;
        IRangedInteractionModule IRangedPlayerInteractableIntegrator.RangedInteractionModule => _toggleActivatable.RangedClickInteractionModule;
        #endregion

        private ToggleActivatable _toggleActivatable = null;

        private void OnEnable()
        {
            string id = "Activatable-" + gameObject.name; 
            _toggleActivatable = new ToggleActivatable(_config, _state, id, ViRSECoreServiceLocator.Instance.WorldStateModulesContainer);
        }

        private void FixedUpdate()
        {
            _toggleActivatable.HandleFixedUpdate();
        }

        private void OnDisable()
        {
            _toggleActivatable.TearDown();
            _toggleActivatable = null;
        }
    }
}