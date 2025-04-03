using System;
using UnityEngine;
using VE2.Core.VComponents.API;
using static VE2.Core.Common.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class ToggleActivatableConfig
    {
        [SerializeField, IgnoreParent] public ToggleActivatableStateConfig StateConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
        [SerializeField, IgnoreParent] public ActivatableRangedInteractionConfig ActivatableRangedInteractionConfig = new();
    }

    [Serializable]
    internal class ActivatableRangedInteractionConfig : RangedInteractionConfig
    {
        [BeginGroup(Style = GroupStyle.Round, ApplyCondition = true)]
        [Title("Ranged Interaction Settings")]
        [SerializeField, IgnoreParent] public bool ActivateAtRangeInVR = true;
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

        //private readonly string _activationGroupID = "None";
        //private readonly bool _isInActivationGroup = false;     
        internal bool test = false;

        public ToggleActivatableService(ToggleActivatableConfig config, VE2Serializable state, string id, IWorldStateSyncService worldStateSyncService, ActivatableGroupsContainer activatableGroupsContainer)
        {
            _StateModule = new(state, config.StateConfig, id, worldStateSyncService,activatableGroupsContainer);

            _RangedClickInteractionModule = new(config.ActivatableRangedInteractionConfig, config.GeneralInteractionConfig, id, config.ActivatableRangedInteractionConfig.ActivateAtRangeInVR);
            _ColliderInteractionModule = new(config.GeneralInteractionConfig, id, CollideInteractionType.Hand);

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
