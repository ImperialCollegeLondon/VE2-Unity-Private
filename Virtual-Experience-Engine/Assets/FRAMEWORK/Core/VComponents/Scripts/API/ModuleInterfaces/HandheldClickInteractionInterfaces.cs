using System;

namespace VE2.Core.VComponents.API
{
    internal interface IHandheldClickInteractionModule : IHandheldInteractionModule
    {
        public void Click(ushort clientID);
        public void ClickUp(ushort clientID);
        public bool IsHoldMode { get; set; }
    }
}

