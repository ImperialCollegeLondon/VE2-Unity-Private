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
        public void OpenDocs() => Application.OpenURL("https://www.notion.so/V_HandHeldActivatable-20f0e4d8ed4d815fadabedf507722d44?source=copy_link");
        [EditorButton(nameof(OpenDocs), "Open Docs", PositionType = ButtonPositionType.Above)]
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
        public ISingleInteractorActivatableStateModule StateModule => _stateModule;
        public IHandheldClickInteractionModule HandheldClickInteractionModule => _handheldClickInteractionModule;
        public IV_FreeGrabbable Grabbable;
        #endregion

        #region Modules
        private readonly SingleInteractorActivatableStateModule _stateModule;
        private readonly HandheldClickInteractionModule _handheldClickInteractionModule;
        #endregion

        public HandheldActivatableService(IV_FreeGrabbable grabbable, HandheldActivatableConfig config, SingleInteractorActivatableState state, string id, IWorldStateSyncableContainer worldStateSyncableContainer,
            ActivatableGroupsContainer activatableGroupsContainer, IClientIDWrapper localClientIdWrapper)
        {
            _stateModule = new(state, config.StateConfig, config.SyncConfig, id, worldStateSyncableContainer, activatableGroupsContainer, localClientIdWrapper);
            _handheldClickInteractionModule = new(grabbable, config.HandheldClickInteractionConfig, config.GeneralInteractionConfig);
            Grabbable = grabbable;

            _handheldClickInteractionModule.OnClickDown += HandleClickDown;
            _handheldClickInteractionModule.OnClickUp += HandleClickUp;
        }

        public void HandleStart() => _stateModule.InitializeStateWithStartingValue();

        public void HandleFixedUpdate() => _stateModule.HandleFixedUpdate();

        private void HandleClickDown(ushort clientID)
        {
            _stateModule.SetNewState(clientID);

            if (_handheldClickInteractionModule.DeactivateOnDrop)
                Grabbable.OnDrop.AddListener(HandleExternalClickUp);
        }

        private void HandleClickUp(ushort clientID)
        {
            if (_handheldClickInteractionModule.IsHoldMode)
            {
                if (_stateModule.IsActivated)
                {
                    _stateModule.SetNewState(clientID);
                }
            }
        }

        private void HandleExternalClickUp()
        {
            if (_handheldClickInteractionModule.IsHoldMode)
                HandleClickUp(Grabbable.MostRecentInteractingClientID.Value);
            else
                HandleClickDown(Grabbable.MostRecentInteractingClientID.Value);
        }

        public void TearDown()
        {
            _stateModule.TearDown();
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
