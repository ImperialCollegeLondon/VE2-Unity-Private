
using System;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class RangedClickInteractionModule : RangedInteractionModule, IRangedClickInteractionModule
    {
        public void ClickDown(ushort clientID)
        {
            //only happens if is valid click
            OnClickDown?.Invoke(clientID);
        }

        public void ClickUp(ushort clientID)
        {
            //only happens if is valid click
            OnClickUp?.Invoke(clientID);
        }

        public event Action<ushort> OnClickDown;
        public event Action<ushort> OnClickUp;

        public RangedClickInteractionModule(RangedInteractionConfig rangedConfig, GeneralInteractionConfig generalConfig) : base(rangedConfig, generalConfig) { }  
    }
}