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
        // Docs button lives in the monobehaviour so it doesn't also appear in the info point insepctor
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
        public ISingleInteractorActivatableStateModule StateModule => _stateModule;
        public IRangedToggleClickInteractionModule RangedClickInteractionModule => _rangedClickInteractionModule;
        public ICollideInteractionModule ColliderInteractionModule => _colliderInteractionModule;
        #endregion

        #region Modules
        private readonly SingleInteractorActivatableStateModule _stateModule;
        private readonly RangedToggleClickInteractionModule _rangedClickInteractionModule;
        private readonly ColliderInteractionModule _colliderInteractionModule;
        #endregion

        public ToggleActivatableService(ToggleActivatableConfig config, SingleInteractorActivatableState state, string id, IWorldStateSyncableContainer worldStateSyncableContainer,
            ActivatableGroupsContainer activatableGroupsContainer, IClientIDWrapper localClientIdWrapper)
        {
            _stateModule = new(state, config.StateConfig, config.SyncConfig, id, worldStateSyncableContainer, activatableGroupsContainer, localClientIdWrapper);

            _rangedClickInteractionModule = new(config.RangedClickInteractionConfig, config.GeneralInteractionConfig, id, config.RangedClickInteractionConfig.ClickAtRangeInVR);

            //Note - yes, this null seems strange on first glance
            //Toggle activatables will sync only via the state module, for hold activatables, interactions are synced via the interactor
            //Since this doesn't apply for toggle activatables, we just pass null here
            _colliderInteractionModule = new(config.CollisionClickInteractionConfig, config.GeneralInteractionConfig, null, id);

            _rangedClickInteractionModule.OnClickDown += HandleInteract;
            _colliderInteractionModule.OnCollideEnter += HandleInteract;
        }

        public void HandleStart() => _stateModule.InitializeStateWithStartingValue();
        public void HandleFixedUpdate() => _stateModule.HandleFixedUpdate();

        private void HandleInteract(InteractorID interactorID)
        {
            _stateModule.SetNewState(interactorID.ClientID);
        }

        public void TearDown()
        {
            _stateModule.TearDown();
        }
    }
}
