using System;
using UnityEngine;
using VE2.Core.VComponents.API;
using static VE2.Core.Common.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class ToggleActivatableConfig
    {
        [SerializeField, IgnoreParent] public ActivatableStateConfig StateConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
        [SerializeField, IgnoreParent] public RangedInteractionConfig RangedInteractionConfig = new();
    }

    internal class ToggleActivatableService
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

        internal bool test = false;

        public ToggleActivatableService(ToggleActivatableConfig config, VE2Serializable state, string id, IWorldStateSyncService worldStateSyncService)
        {
            _StateModule = new(state, config.StateConfig, id, worldStateSyncService);
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
