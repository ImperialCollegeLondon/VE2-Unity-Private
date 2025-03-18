
using System;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class RangedClickInteractionModule : RangedInteractionModule, IRangedClickInteractionModule
    {
        public void ClickDown(InteractorID interactorID)
        {
            //only happens if is valid click
            OnClickDown?.Invoke(interactorID);
        }

        public void ClickUp(InteractorID interactorID)
        {
            //only happens if is valid click
            OnClickUp?.Invoke(interactorID);
        }
        public string ID { get; }

        public event Action<InteractorID> OnClickDown;
        public event Action<InteractorID> OnClickUp;

        public RangedClickInteractionModule(RangedInteractionConfig rangedConfig, GeneralInteractionConfig generalConfig, string id) : base(rangedConfig, generalConfig) 
        {
            ID = id;
        }  
    }
}