using System;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class HoldActivatableConfig
    {
        [SerializeField, IgnoreParent] public HoldActivatableStateConfig StateConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
        [SerializeField, IgnoreParent] public ActivatableInteractionConfig ActivatableRangedInteractionConfig = new();
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

        public HoldActivatableService(HoldActivatableConfig config, MultiInteractorActivatableState state, string id, IClientIDWrapper localClientIdWrapper)
        {
            _StateModule = new(state, config.StateConfig, id, localClientIdWrapper);
            _RangedHoldClickInteractionModule = new(config.ActivatableRangedInteractionConfig, config.GeneralInteractionConfig, id, config.ActivatableRangedInteractionConfig.ActivateAtRangeInVR);

            if(config.ActivatableRangedInteractionConfig.ActivateWithCollisionInVR)
                _ColliderInteractionModule = new(config.GeneralInteractionConfig, id, CollideInteractionType.Hand);
            else
                _ColliderInteractionModule = new(config.GeneralInteractionConfig, id, CollideInteractionType.None);

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