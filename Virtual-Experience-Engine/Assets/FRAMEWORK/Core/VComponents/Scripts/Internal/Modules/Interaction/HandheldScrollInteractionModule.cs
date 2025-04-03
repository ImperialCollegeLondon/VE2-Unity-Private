using System;
using VE2.Core.VComponents.API;


namespace VE2.Core.VComponents.Internal
{
    internal class HandheldScrollInteractionModule: GeneralInteractionModule, IHandheldScrollInteractionModule
    {
        public event Action<ushort> OnScrollUp;
        public event Action<ushort> OnScrollDown;
    
        public HandheldScrollInteractionModule(GeneralInteractionConfig config) : base(config) { }

        public void ScrollUp(ushort clientID) => OnScrollUp?.Invoke(clientID);

        public void ScrollDown(ushort clientID) => OnScrollDown?.Invoke(clientID);
    }

}
