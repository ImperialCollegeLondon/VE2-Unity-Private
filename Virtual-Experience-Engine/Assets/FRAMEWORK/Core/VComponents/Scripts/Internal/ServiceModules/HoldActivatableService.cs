using System;
using UnityEngine;
using UnityEngine.Events;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class HoldActivatableConfig
    {
        [SerializeField, IgnoreParent] public HoldActivatableStateConfig StateConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
        [SerializeField, IgnoreParent] public ActivatableRangedInteractionConfig ActivatableRangedInteractionConfig = new();
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

        public HoldActivatableService(HoldActivatableConfig config, MultiInteractorActivatableState state, string id)
        {
            _StateModule = new(state, config.StateConfig, id);
            _RangedHoldClickInteractionModule = new(config.ActivatableRangedInteractionConfig, config.GeneralInteractionConfig, id, config.ActivatableRangedInteractionConfig.ActivateAtRangeInVR);
            _ColliderInteractionModule = new(config.GeneralInteractionConfig, id, CollideInteractionType.Hand);

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