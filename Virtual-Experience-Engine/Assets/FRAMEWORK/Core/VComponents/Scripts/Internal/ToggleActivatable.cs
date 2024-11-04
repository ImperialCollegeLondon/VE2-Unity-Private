using System;
using UnityEngine;
using VE2.Common;
using VE2.Core.VComponents.NonInteractableInterfaces;
using VE2.Core.VComponents.InteractableInterfaces;
using static VE2.Common.CoreCommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    public class ToggleActivatableConfig
    {
        [SerializeField, IgnoreParent] public ActivatableStateConfig StateConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
        [SerializeField, IgnoreParent] public RangedInteractionConfig RangedInteractionConfig = new();
    }

    public class ToggleActivatable
    {
        #region Interfaces
        public ISingleInteractorActivatableStateModule StateModule => _StateModule;
        public IRangedClickInteractionModule RangedClickInteractionModule => _RangedClickInteractionModule;
        public ICollideInteractionModule ColliderInteractionModule => _ColliderInteractionModule;
        #endregion

        #region Modules
        private readonly SingleInteractorActivatableStateModule _StateModule;
        private readonly RangedClickInteractionModule _RangedClickInteractionModule;
        private readonly ColliderInteractionModule _ColliderInteractionModule;
        #endregion

        public ToggleActivatable(ToggleActivatableConfig config, VE2Serializable state, string id, WorldStateModulesContainer worldStateModulesContainer)
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
