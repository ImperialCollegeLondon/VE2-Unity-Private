using System;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Shared;
namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class PressurePlateConfig
    {
        public void OpenDocs() => Application.OpenURL("https://www.notion.so/V_PressurePlate-2150e4d8ed4d80a58decf17b24427a78?source=copy_link");
        [EditorButton(nameof(OpenDocs), "Open Docs", PositionType = ButtonPositionType.Above)]
        [SerializeField, IgnoreParent] public HoldActivatableStateConfig StateConfig = new();

        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();

        [HideIf(nameof(MultiplayerSupportPresent), false)]
        [SerializeField, IgnoreParent] internal WorldStateSyncConfig SyncConfig = new();

        private bool MultiplayerSupportPresent => VE2API.HasMultiPlayerSupport;
    }
    
    internal class PressurePlateService
    {
        #region Interfaces
        public IMultiInteractorActivatableStateModule StateModule => _StateModule;
        public ICollideInteractionModule ColliderInteractionModule => _ColliderInteractionModule;
        #endregion

        #region Modules
        private readonly MultiInteractorActivatableStateModule _StateModule;
        private readonly ColliderInteractionModule _ColliderInteractionModule;
        #endregion

        public PressurePlateService(PressurePlateConfig config, MultiInteractorActivatableSyncedState state, string id, IClientIDWrapper localClientIdWrapper, IWorldStateSyncableContainer worldStateSyncableContainer)
        {
            _StateModule = new(state, config.StateConfig, id, localClientIdWrapper, config.SyncConfig, worldStateSyncableContainer);
            _ColliderInteractionModule = new(CollideInteractionType.Feet, config.GeneralInteractionConfig, config.SyncConfig, id);

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
