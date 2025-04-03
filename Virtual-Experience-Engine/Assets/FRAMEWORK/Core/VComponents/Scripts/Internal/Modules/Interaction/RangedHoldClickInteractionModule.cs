using System;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class RangedHoldClickInteractionModule : RangedToggleClickInteractionModule, IRangedHoldClickInteractionModule
    {
        public void ClickUp(InteractorID interactorID)
        {
            OnClickUp?.Invoke(interactorID);
        }

        public event Action<InteractorID> OnClickUp;

        public RangedHoldClickInteractionModule(RangedInteractionConfig rangedConfig, GeneralInteractionConfig generalConfig, string id, bool activateAtRangeInVR)
            : base(rangedConfig, generalConfig, id, activateAtRangeInVR) { }
    }
}
