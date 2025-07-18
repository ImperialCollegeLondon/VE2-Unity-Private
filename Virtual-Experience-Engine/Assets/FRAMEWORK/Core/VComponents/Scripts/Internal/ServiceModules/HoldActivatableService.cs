using System;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Shared;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class HoldActivatableConfig
    {
        public void OpenDocs() => Application.OpenURL("https://www.notion.so/V_HoldActivatable-20f0e4d8ed4d816db96ee5435ddf9f77?source=copy_link");
        [EditorButton(nameof(OpenDocs), "Open Docs", PositionType = ButtonPositionType.Above)]
        [SerializeField, IgnoreParent] public HoldActivatableStateConfig StateConfig = new();

        [SerializeField, IgnoreParent] public CollisionClickInteractionConfig CollisionClickInteractionConfig = new();
        [SerializeField, IndentArea(-1)] public RangedClickInteractionConfig ActivatableRangedInteractionConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();

        [HideIf(nameof(MultiplayerSupportPresent), false)]
        [SerializeField, IgnoreParent] internal WorldStateSyncConfig SyncConfig = new();

        private bool MultiplayerSupportPresent => VE2API.HasMultiPlayerSupport;
    }

    internal class HoldActivatableService
    {
        #region Interfaces
        public IMultiInteractorActivatableStateModule StateModule => _StateModule;
        public IRangedHoldClickInteractionModule RangedClickInteractionModule => _RangedHoldClickInteractionModule;
        public ICollideInteractionModule ColliderInteractionModule => _ColliderInteractionModule;
        #endregion

        #region Modules
        private readonly MultiInteractorActivatableStateModule _StateModule;
        private readonly RangedHoldClickInteractionModule _RangedHoldClickInteractionModule;
        private readonly ColliderInteractionModule _ColliderInteractionModule;
        #endregion

        public HoldActivatableService(HoldActivatableConfig config, MultiInteractorActivatableSyncedState state, string id, IClientIDWrapper localClientIdWrapper, IWorldStateSyncableContainer worldStateSyncableContainer)
        {
            _StateModule = new(state, config.StateConfig, id, localClientIdWrapper, config.SyncConfig, worldStateSyncableContainer);

            _RangedHoldClickInteractionModule = new(config.ActivatableRangedInteractionConfig, config.GeneralInteractionConfig, config.SyncConfig, id, config.ActivatableRangedInteractionConfig.ClickAtRangeInVR);
            _ColliderInteractionModule = new(config.CollisionClickInteractionConfig, config.GeneralInteractionConfig, config.SyncConfig, id);

            _RangedHoldClickInteractionModule.OnClickDown += AddToInteractingInteractors;
            _RangedHoldClickInteractionModule.OnClickUp += RemoveFromInteractingInteractors;
            
            _ColliderInteractionModule.OnCollideEnter += AddToInteractingInteractors;
            _ColliderInteractionModule.OnCollideExit += RemoveFromInteractingInteractors;
        }

        public void HandleFixedUpdate()
        {
            _StateModule.HandleFixedUpdate();
        }

        private void AddToInteractingInteractors(InteractorID interactorID)
        {
            _StateModule.AddInteractorToState(interactorID);
        }

        private void RemoveFromInteractingInteractors(InteractorID interactorID)
        {
            _StateModule.RemoveInteractorFromState(interactorID);
        }

        public void TearDown() 
        {
            _StateModule.TearDown();
        }
    }
}