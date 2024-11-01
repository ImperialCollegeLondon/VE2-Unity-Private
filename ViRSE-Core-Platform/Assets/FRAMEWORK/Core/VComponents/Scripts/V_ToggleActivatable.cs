using System;
using System.Linq;
using UnityEngine;
using ViRSE.Common;
using ViRSE.Core.VComponents.InternalInterfaces;
using ViRSE.Core.VComponents.PlayerInterfaces;
using ViRSE.Core.VComponents.PluginInterfaces;
using static ViRSE.Common.CoreCommonSerializables;

namespace ViRSE.Core.VComponents
{
    [Serializable]
    internal class ToggleActivatableConfig
    {
        [SerializeField, IgnoreParent] public ActivatableStateConfig StateConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
        [SerializeField, IgnoreParent] public RangedInteractionConfig RangedInteractionConfig = new();
    }

    internal class V_ToggleActivatable : MonoBehaviour, IV_ToggleActivatable, IRangedClickPlayerInteractable, ICollidePlayerInteractable
    {
        [SerializeField, HideLabel, IgnoreParent] private ToggleActivatableConfig _config = new(); 
        [SerializeField, HideInInspector] private SingleInteractorActivatableState _state = new();

        #region Plugin Interfaces
        ISingleInteractorActivatableStateModuleImplementor ISingleInteractorStateModulePluginInterface._StateModuleImplementor => _toggleActivatable;
        IRangedInteractionModuleImplementor IRangedInteractablePluginInterface._RangedModuleImplementor => _toggleActivatable;
        IGeneralInteractionModuleImplementor IGeneralInteractionPluginInterface._GeneralModuleImplementor => _toggleActivatable;
        #endregion

        #region Player Interfaces
        IRangedInteractionModuleImplementor IRangedPlayerInteractable.RangedModuleImplementor => _toggleActivatable;
        IGeneralInteractionModuleImplementor IGeneralPlayerInteractable._GeneralModuleImplementor => _toggleActivatable;
        ICollideInteractionModuleImplementor ICollidePlayerInteractable._CollideModuleImplementor => _toggleActivatable;
        #endregion

        private ToggleActivatable _toggleActivatable = null;

        private void OnEnable()
        {
            string id = "Activatable-" + gameObject.name; 
            _toggleActivatable = ToggleActivatableFactory.Create(_config, _state, id);
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

    internal static class ToggleActivatableFactory
    {
        public static ToggleActivatable Create(ToggleActivatableConfig config, ViRSESerializable state, string id)
        {
            return new ToggleActivatable(config, state, id, ViRSECoreServiceLocator.Instance.WorldStateModulesContainer);
        }
    }

    internal class ToggleActivatable : ISingleInteractorActivatableStateModuleImplementor, IRangedClickInteractionModuleImplementor, ICollideInteractionModuleImplementor
    {
        #region Interfaces
        ISingleInteractorActivatableStateModule ISingleInteractorActivatableStateModuleImplementor.StateModule => _stateModule;
        IRangedInteractionModule IRangedInteractionModuleImplementor.RangedInteractionModule => _rangedClickInteractionModule;
        ICollideInteractionModule ICollideInteractionModuleImplementor.CollideInteractionModule => _colliderInteractionModule;
        IGeneralInteractionModule IGeneralInteractionModuleImplementor.GeneralInteractionModule => _rangedClickInteractionModule;
        #endregion

        #region Modules
        private readonly SingleInteractorActivatableStateModule _stateModule;
        private readonly RangedClickInteractionModule _rangedClickInteractionModule;
        private readonly ColliderInteractionModule _colliderInteractionModule;
        #endregion

        public ToggleActivatable(ToggleActivatableConfig config, ViRSESerializable state, string id, WorldStateModulesContainer worldStateModulesContainer)
        {
            _stateModule = new(state, config.StateConfig, id, worldStateModulesContainer);
            _rangedClickInteractionModule = new(config.RangedInteractionConfig, config.GeneralInteractionConfig);
            _colliderInteractionModule = new(config.GeneralInteractionConfig);

            _rangedClickInteractionModule.OnClickDown += HandleInteract;
            _colliderInteractionModule.OnCollideEnter += HandleInteract;
        }

        public void HandleFixedUpdate()
        {
            _stateModule.HandleFixedUpdate();
        }

        private void HandleInteract(ushort clientID)
        {
            _stateModule.InvertState(clientID);
        }

        public void TearDown() 
        {
            _stateModule.TearDown();
        }
    }
}
