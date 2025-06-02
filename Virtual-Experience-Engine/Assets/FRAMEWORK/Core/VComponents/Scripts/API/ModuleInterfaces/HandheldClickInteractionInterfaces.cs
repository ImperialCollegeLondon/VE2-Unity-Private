using System;

namespace VE2.Core.VComponents.API
{
    internal interface IHandheldClickInteractionModule : IHandheldInteractionModule
    {
        public void ClickDown(ushort clientID);
        public void ClickUp(ushort clientID);
        public bool IsHoldMode { get;}
        public bool DeactivateOnDrop { get;} //If true, the activatable will deactivate when the handheld is dropped  
    }
}

