using System;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class PressurePlateConfig
    {
        [SerializeField, IgnoreParent] public HoldActivatableStateConfig StateConfig = new();

        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();

        [HideIf(nameof(MultiplayerSupportPresent), false)]
        [SerializeField, IgnoreParent] internal HoldActivatablePlayerSyncIndicator SyncConfig = new();

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

        public PressurePlateService(PressurePlateConfig config, MultiInteractorActivatableState state, string id, IClientIDWrapper localClientIdWrapper)
        {
            _StateModule = new(state, config.StateConfig, id, localClientIdWrapper);
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
