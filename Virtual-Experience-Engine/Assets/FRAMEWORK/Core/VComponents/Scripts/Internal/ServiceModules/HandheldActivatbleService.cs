using UnityEngine;
using System;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using static VE2.Core.Common.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class HandheldActivatableConfig
    {
        [SerializeField, IgnoreParent] public ToggleActivatableStateConfig StateConfig = new();
        [SerializeField, IgnoreParent] public HandHeldClickInteractionConfig HandheldClickInteractionConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
    }
    internal class HandheldActivatableService
    {
        #region Interfaces
        public ISingleInteractorActivatableStateModule StateModule => _StateModule;
        public IHandheldClickInteractionModule HandheldClickInteractionModule => _HandheldClickInteractionModule;
        public IV_FreeGrabbable Grabbable;
        #endregion

        #region Modules
        private readonly SingleInteractorActivatableStateModule _StateModule;
        private readonly HandheldClickInteractionModule _HandheldClickInteractionModule;
        #endregion

        public HandheldActivatableService(IV_FreeGrabbable grabbable,HandheldActivatableConfig config, VE2Serializable state, string id, IWorldStateSyncService worldStateSyncService, ActivatableGroupsContainer activatableGroupsContainer)
        {
            _StateModule = new(state, config.StateConfig, id, worldStateSyncService, activatableGroupsContainer);
            _HandheldClickInteractionModule = new(grabbable, config.HandheldClickInteractionConfig, config.GeneralInteractionConfig);
            Grabbable = grabbable;

            _HandheldClickInteractionModule.OnClickDown += HandleClickDown;
            _HandheldClickInteractionModule.OnClickUp += HandleClickUp;

        }

        public void HandleFixedUpdate()
        {
            _StateModule.HandleFixedUpdate();
        }

        private void HandleClickDown(ushort clientID)
        {
            _StateModule.SetNewState(clientID);

            if (_HandheldClickInteractionModule.DeactivateOnDrop)
                Grabbable.OnDrop.AddListener(HandleExternalClickUp);
        }

        private void HandleClickUp(ushort clientID)
        {
            if (_HandheldClickInteractionModule.IsHoldMode)
            {
                if (_StateModule.IsActivated)
                {
                    _StateModule.SetNewState(clientID);
                }
            }
        }

        private void HandleExternalClickUp()
        {
            if (_HandheldClickInteractionModule.IsHoldMode)
                HandleClickUp(Grabbable.MostRecentInteractingClientID);
            else
                HandleClickDown(Grabbable.MostRecentInteractingClientID);
        }

        public void TearDown()
        {
            _StateModule.TearDown();
        }
    }

    [Serializable]
    internal class HandHeldClickInteractionConfig
    {
        [Title("Interaction Settings")]
        [BeginGroup(Style = GroupStyle.Round, ApplyCondition = false)]
        [SerializeField] internal bool IsHoldMode = false;

        [EndGroup(ApplyCondition = false)]
        [SerializeField] internal bool DeactivateOnDrop = true;   
    }
}
