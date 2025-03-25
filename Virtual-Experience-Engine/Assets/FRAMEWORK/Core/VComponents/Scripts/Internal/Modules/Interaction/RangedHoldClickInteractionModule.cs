using System;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class RangedHoldClickInteractionModule : RangedInteractionModule, IRangedHoldClickInteractionModule
    {
        public void ClickDown(InteractorID interactorID)
        {
            OnClickDown?.Invoke(interactorID);
        }

        public void ClickUp(InteractorID interactorID)
        {
            OnClickUp?.Invoke(interactorID);
        }

        public string ID { get; }

        public event Action<InteractorID> OnClickDown;
        public event Action<InteractorID> OnClickUp;

        public RangedHoldClickInteractionModule(RangedInteractionConfig rangedConfig, GeneralInteractionConfig generalConfig, string id) : base(rangedConfig, generalConfig) 
        {
            ID = id;
        }  
    }
}
