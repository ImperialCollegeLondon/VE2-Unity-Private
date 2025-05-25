using System;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Shared;
using static VE2.Common.Shared.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class ToggleActivatableConfig
    {
        [SerializeField, IgnoreParent] public ToggleActivatableStateConfig StateConfig = new();
        
        [SerializeField, IgnoreParent] public CollisionClickInteractionConfig CollisionClickInteractionConfig = new();
        [SerializeField, IndentArea(-1)] public RangedClickInteractionConfig RangedClickInteractionConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
        
        [HideIf(nameof(MultiplayerSupportPresent), false)]
        [SerializeField, IgnoreParent] public WorldStateSyncConfig SyncConfig = new();

        private bool MultiplayerSupportPresent => VE2API.HasMultiPlayerSupport;
    }

    internal class ToggleActivatableService
    {
        #region Interfaces
        public ISingleInteractorActivatableStateModule StateModule => _StateModule;
        public IRangedToggleClickInteractionModule RangedClickInteractionModule => _RangedClickInteractionModule;
        public ICollideInteractionModule ColliderInteractionModule => _ColliderInteractionModule;
        #endregion

        #region Modules
        private readonly SingleInteractorActivatableStateModule _StateModule;
        private readonly RangedToggleClickInteractionModule _RangedClickInteractionModule;
        private readonly ColliderInteractionModule _ColliderInteractionModule;
        #endregion

        public ToggleActivatableService(ToggleActivatableConfig config, VE2Serializable state, string id, IWorldStateSyncableContainer worldStateSyncableContainer,
            ActivatableGroupsContainer activatableGroupsContainer, IClientIDWrapper localClientIdWrapper)
        {
            _StateModule = new(state, config.StateConfig, config.SyncConfig, id, worldStateSyncableContainer, activatableGroupsContainer, localClientIdWrapper);

            _RangedClickInteractionModule = new(config.RangedClickInteractionConfig, config.GeneralInteractionConfig, id, config.RangedClickInteractionConfig.ClickAtRangeInVR);

            /*
                What's the deal here...?
                Toggle activatables should probably use a "ToggleCollisionInteractionModule" instead of a "ColliderInteractionModule",
                That way, we wont worry about the interactor storing that coll interface in its list of held interactables...
                that held activatables list is for syncing, toggle activatables sync through their state module!

            */
            _ColliderInteractionModule = new(config.CollisionClickInteractionConfig, config.GeneralInteractionConfig, config.SyncConfig, id);

            _RangedClickInteractionModule.OnClickDown += HandleInteract;
            _ColliderInteractionModule.OnCollideEnter += HandleInteract;

            _StateModule.SetActivated(config.StateConfig.ActivateOnStart);
        }

        public void HandleFixedUpdate()
        {
            _StateModule.HandleFixedUpdate();
        }

        private void HandleInteract(InteractorID interactorID)
        {
            _StateModule.ToggleActivatableState(interactorID.ClientID);
        }

        public void TearDown()
        {
            _StateModule.TearDown();
        }
    }
}
