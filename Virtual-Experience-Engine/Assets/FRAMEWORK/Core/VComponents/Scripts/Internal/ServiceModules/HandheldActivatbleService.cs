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

            // Wire differently based on whether we’re in hold or toggle mode.
            if (_HandheldClickInteractionModule.IsHoldMode)
            {
                _HandheldClickInteractionModule.OnClick += HandleClick;
                _HandheldClickInteractionModule.OnClickUp += HandleClickUp;
            }
            else
            {
                // In toggle mode, a click simply toggles the state.
                _HandheldClickInteractionModule.OnClick += HandleToggle;
            }
        }

        public void HandleFixedUpdate()
        {
            _StateModule.HandleFixedUpdate();
        }

        private void HandleClick(ushort clientID)
        { 
            if (!_StateModule.IsActivated)
            {
                _StateModule.SetHoldActivatableState(clientID, true);

                if (_HandheldClickInteractionModule.DeactivateOnDrop)
                {
                    Grabbable.OnDrop.AddListener(HandleExternalClickUp);
                }
            }

        }

        private void HandleClickUp(ushort clientID)
        {  
            if (_StateModule.IsActivated)
            {
                _StateModule.SetHoldActivatableState(clientID, false);
            }
        }

        private void HandleExternalClickUp()
        {
            if (_HandheldClickInteractionModule.IsHoldMode)
            {
                HandleClickUp(Grabbable.MostRecentInteractingClientID);
            }
            else
            {
                HandleToggle(Grabbable.MostRecentInteractingClientID);
            }
        }

        private void HandleToggle(ushort clientID)
        {   
            Debug.Log("Handle Toggle Click");
            _StateModule.ToggleActivatableState(clientID);
            if (_HandheldClickInteractionModule.DeactivateOnDrop)
            {
                Debug.Log("Handle Toggle Click - DeactivateOnDrop");
                Grabbable.OnDrop.AddListener(HandleExternalClickUp);
            }
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
