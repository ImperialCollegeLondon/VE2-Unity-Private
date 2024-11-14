using System;

namespace VE2.Core.VComponents.InteractableInterfaces
{ 
    public interface IHandheldClickInteractionModule : IHandheldInteractionModule
    {
        public void Click(ushort clientID);
    }
}

