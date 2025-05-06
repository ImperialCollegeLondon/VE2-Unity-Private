using System;
using UnityEngine;
using VE2.Common.Shared;
using VE2.Core.Common;
using VE2.Core.VComponents.API;
using static VE2.Common.Shared.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class ToggleActivatableConfig
    {
        [SerializeField, IgnoreParent] public ToggleActivatableStateConfig StateConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
        [SerializeField, IgnoreParent] public ActivatableInteractionConfig ActivatableRangedInteractionConfig = new();
    }

    [Serializable]
    internal class ActivatableInteractionConfig : RangedInteractionConfig
    {
        [BeginGroup(Style = GroupStyle.Round, ApplyCondition = true)]
        [Title("Activatable Ranged Interaction Settings")]
        [SerializeField, IgnoreParent] public bool ActivateAtRangeInVR = true;
        [SerializeField, IgnoreParent, EndGroup] public bool ActivateWithCollisionInVR= true;
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
            _StateModule = new(state, config.StateConfig, id, worldStateSyncableContainer, activatableGroupsContainer, localClientIdWrapper);

            _RangedClickInteractionModule = new(config.ActivatableRangedInteractionConfig, config.GeneralInteractionConfig, id, config.ActivatableRangedInteractionConfig.ActivateAtRangeInVR);

            if(config.ActivatableRangedInteractionConfig.ActivateWithCollisionInVR)
                _ColliderInteractionModule = new(config.GeneralInteractionConfig, id, CollideInteractionType.Hand);
            else
                _ColliderInteractionModule = new(config.GeneralInteractionConfig, id, CollideInteractionType.None);

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
