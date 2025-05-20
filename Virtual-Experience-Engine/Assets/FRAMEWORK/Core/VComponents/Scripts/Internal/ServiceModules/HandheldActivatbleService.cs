using UnityEngine;
using System;
using VE2.Core.VComponents.API;
using static VE2.Common.Shared.CommonSerializables;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class HandheldActivatableConfig
    {
        [SerializeField, IgnoreParent] public ToggleActivatableStateConfig StateConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
    }
    internal class HandheldActivatableService
    {
        #region Interfaces
        public ISingleInteractorActivatableStateModule StateModule => _StateModule;
        public IHandheldClickInteractionModule HandheldClickInteractionModule => _HandheldClickInteractionModule;
        #endregion

        #region Modules
        private readonly SingleInteractorActivatableStateModule _StateModule;
        private readonly HandheldClickInteractionModule _HandheldClickInteractionModule;
        #endregion

        public HandheldActivatableService(HandheldActivatableConfig config, VE2Serializable state, string id, IWorldStateSyncableContainer worldStateSyncableContainer, 
            ActivatableGroupsContainer activatableGroupsContainer, IClientIDWrapper localClientIdWrapper)
        {
            _StateModule = new(state, config.StateConfig, id, worldStateSyncableContainer, activatableGroupsContainer, localClientIdWrapper);
            _HandheldClickInteractionModule = new(config.GeneralInteractionConfig);

            _HandheldClickInteractionModule.OnClickDown += HandleInteract;
        }

        public void HandleFixedUpdate()
        {
            _StateModule.HandleFixedUpdate();
        }

        private void HandleInteract(ushort clientID)
        {
            _StateModule.ToggleActivatableState(clientID);
        }

        public void TearDown()
        {
            _StateModule.TearDown();
        }
    }
}
