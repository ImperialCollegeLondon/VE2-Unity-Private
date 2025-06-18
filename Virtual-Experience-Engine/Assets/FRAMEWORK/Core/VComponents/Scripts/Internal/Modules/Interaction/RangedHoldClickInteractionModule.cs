using System;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Shared;

namespace VE2.Core.VComponents.Internal
{
    internal class RangedHoldClickInteractionModule : RangedToggleClickInteractionModule, IRangedHoldClickInteractionModule
    {
        public bool IsNetworked => _syncConfig.IsNetworked;
        public void ClickUp(InteractorID interactorID) => OnClickUp?.Invoke(interactorID);

        internal event Action<InteractorID> OnClickUp;

        private readonly WorldStateSyncConfig _syncConfig;

        //TODO: activateAtRangeInVR should be wrapped in a config class
        public RangedHoldClickInteractionModule(RangedInteractionConfig rangedConfig, GeneralInteractionConfig generalConfig, WorldStateSyncConfig syncConfig,
            string id, bool activateAtRangeInVR) : base(rangedConfig, generalConfig, id, activateAtRangeInVR)
        {
            _syncConfig = syncConfig;
        }
    }
}
