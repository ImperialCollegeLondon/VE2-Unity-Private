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
        public void OpenDocs() => Application.OpenURL("https://www.notion.so/V_ToggleActivatable-2130e4d8ed4d80fcb471cc08f80acc56?source=copy_link");
        [EditorButton(nameof(OpenDocs), "Open Docs", PositionType = ButtonPositionType.Above)]
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

        public ToggleActivatableService(ToggleActivatableConfig config, SingleInteractorActivatableState state, string id, IWorldStateSyncableContainer worldStateSyncableContainer,
            ActivatableGroupsContainer activatableGroupsContainer, IClientIDWrapper localClientIdWrapper)
        {
            _StateModule = new(state, config.StateConfig, config.SyncConfig, id, worldStateSyncableContainer, activatableGroupsContainer, localClientIdWrapper);

            _RangedClickInteractionModule = new(config.RangedClickInteractionConfig, config.GeneralInteractionConfig, id, config.RangedClickInteractionConfig.ClickAtRangeInVR);

            //Note - yes, this null seems strange on first glance
            //Toggle activatables will sync only via the state module, for hold activatables, interactions are synced via the interactor
            //Since this doesn't apply for toggle activatables, we just pass null here
            _ColliderInteractionModule = new(config.CollisionClickInteractionConfig, config.GeneralInteractionConfig, null, id);

            _RangedClickInteractionModule.OnClickDown += HandleInteract;
            _ColliderInteractionModule.OnCollideEnter += HandleInteract;

            if (!state.IsInitialised && config.StateConfig.ActivateOnStart)
                _StateModule.SetActivated(config.StateConfig.ActivateOnStart);

            state.IsInitialised = true;
        }

        public void HandleFixedUpdate()
        {
            _StateModule.HandleFixedUpdate();
        }

        private void HandleInteract(InteractorID interactorID)
        {
            _StateModule.SetNewState(interactorID.ClientID);
        }

        public void TearDown()
        {
            _StateModule.TearDown();
        }
    }
}
