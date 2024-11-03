using System;
using System.Linq;
using UnityEngine;
using ViRSE.Common;
using ViRSE.Core.VComponents.Internal;
using ViRSE.Core.VComponents.NonInteractableInterfaces;
using ViRSE.Core.VComponents.RaycastInterfaces;
using ViRSE.Core.VComponents.PluginInterfaces;
using VIRSE.Core.VComponents.InteractableInterfaces;
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

    internal class ToggleActivatable
    {
        public ISingleInteractorActivatableStateModule StateModule => _StateModule;
        public IRangedClickInteractionModule RangedClickInteractionModule => _RangedClickInteractionModule;
        public ICollideInteractionModule ColliderInteractionModule => _ColliderInteractionModule;

        #region Modules
        private readonly SingleInteractorActivatableStateModule _StateModule;
        private readonly RangedClickInteractionModule _RangedClickInteractionModule;
        private readonly ColliderInteractionModule _ColliderInteractionModule;
        #endregion

        public ToggleActivatable(ToggleActivatableConfig config, ViRSESerializable state, string id, WorldStateModulesContainer worldStateModulesContainer)
        {
            _StateModule = new(state, config.StateConfig, id, worldStateModulesContainer);
            _RangedClickInteractionModule = new(config.RangedInteractionConfig, config.GeneralInteractionConfig);
            _ColliderInteractionModule = new(config.GeneralInteractionConfig);

            _RangedClickInteractionModule.OnClickDown += HandleInteract;
            _ColliderInteractionModule.OnCollideEnter += HandleInteract;
        }

        public void HandleFixedUpdate()
        {
            _StateModule.HandleFixedUpdate();
        }

        private void HandleInteract(ushort clientID)
        {
            _StateModule.InvertState(clientID);
        }

        public void TearDown() 
        {
            _StateModule.TearDown();
        }
    }
}
