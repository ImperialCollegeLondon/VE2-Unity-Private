using System;
using UnityEngine;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class HoldActivatableConfig
    {
        [SerializeField, IgnoreParent] public ActivatableStateConfig StateConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
        [SerializeField, IgnoreParent] public RangedInteractionConfig RangedInteractionConfig = new();
    }
    internal class HoldActivatableSerice
    {
        #region Interfaces
        public IMultiInteractorActivatableStateModule StateModule => _StateModule;
        public IRangedClickInteractionModule RangedClickInteractionModule => _RangedClickInteractionModule;
        public ICollideInteractionModule ColliderInteractionModule => _ColliderInteractionModule;
        #endregion

        #region Modules
        private readonly MultiInteractorActivatableStateModule _StateModule;
        private readonly RangedClickInteractionModule _RangedClickInteractionModule;
        private readonly ColliderInteractionModule _ColliderInteractionModule;
        #endregion

        public HoldActivatableSerice(ToggleActivatableConfig config, MultiInteractorActivatableState state, string id, IWorldStateSyncService worldStateSyncService)
        {
            _StateModule = new(state, config.StateConfig, id);
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