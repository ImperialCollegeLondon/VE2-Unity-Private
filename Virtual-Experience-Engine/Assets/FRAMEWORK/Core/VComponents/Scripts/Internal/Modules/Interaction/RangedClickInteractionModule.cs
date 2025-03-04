
using System;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class RangedClickInteractionModule : RangedInteractionModule, IRangedClickInteractionModule
    {
        public void Click(ushort clientID)
        {
            //only happens if is valid click
            OnClickDown?.Invoke(clientID);
        }

        public event Action<ushort> OnClickDown;

        public RangedClickInteractionModule(RangedInteractionConfig rangedConfig, GeneralInteractionConfig generalConfig) : base(rangedConfig, generalConfig) { }  
    }
}