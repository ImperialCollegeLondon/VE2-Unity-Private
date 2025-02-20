using UnityEngine;
using System;
using VE2.Core.VComponents.API;
using static VE2.Core.Common.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class HandheldActivatableConfig
    {
        [SerializeField, IgnoreParent] public ActivatableStateConfig StateConfig = new();
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

        public HandheldActivatableService(HandheldActivatableConfig config, VE2Serializable state, string id, IWorldStateSyncService worldStateSyncService)
        {
            _StateModule = new(state, config.StateConfig, id, worldStateSyncService);
            _HandheldClickInteractionModule = new(config.GeneralInteractionConfig);

            _HandheldClickInteractionModule.OnClickDown += HandleInteract;
        }

        public void HandleFixedUpdate()
        {
            _StateModule.HandleFixedUpdate();
        }

        private void HandleInteract(ushort clientID)
        {
            _StateModule.InvertState(clientID);
        }

        public void TearDown()
        {
            _StateModule.TearDown();
        }
    }
}
