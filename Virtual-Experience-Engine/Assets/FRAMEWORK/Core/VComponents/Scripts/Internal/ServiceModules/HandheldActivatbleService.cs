using UnityEngine;
using System;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using static VE2.Common.Shared.CommonSerializables;
using VE2.Common.Shared;
using VE2.Core.VComponents.Shared;
using VE2.Common.API;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class HandheldActivatableConfig
    {
        [SerializeField, IgnoreParent] public ToggleActivatableStateConfig StateConfig = new();

        [SerializeField, IgnoreParent] public HandHeldClickInteractionConfig HandheldClickInteractionConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
        
        [HideIf(nameof(MultiplayerSupportPresent), false)]
        [SerializeField, IgnoreParent] public WorldStateSyncConfig SyncConfig = new();

        private bool MultiplayerSupportPresent => VE2API.HasMultiPlayerSupport;
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

        public HandheldActivatableService(IV_FreeGrabbable grabbable, HandheldActivatableConfig config, VE2Serializable state, string id, IWorldStateSyncableContainer worldStateSyncableContainer, 
            ActivatableGroupsContainer activatableGroupsContainer, IClientIDWrapper localClientIdWrapper)
        {
            _StateModule = new(state, config.StateConfig, config.SyncConfig, id, worldStateSyncableContainer, activatableGroupsContainer, localClientIdWrapper);
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
                HandleClickUp(Grabbable.MostRecentInteractingClientID.Value);
            else
                HandleClickDown(Grabbable.MostRecentInteractingClientID.Value);
        }

        public void TearDown()
        {
            _StateModule.TearDown();
        }
    }

    [Serializable]
    internal class HandHeldClickInteractionConfig
    {
        [Title("Handheld Click Interaction Settings")]
        [BeginGroup(Style = GroupStyle.Round, ApplyCondition = false)]
        [SerializeField] internal bool IsHoldMode = false;

        [EndGroup(ApplyCondition = false)]
        [SerializeField] internal bool DeactivateOnDrop = true;   
    }
}
