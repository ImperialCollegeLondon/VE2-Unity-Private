using System;

namespace VE2.Core.VComponents.API
{
    internal interface IHandheldClickInteractionModule : IHandheldInteractionModule
    {
        public void Click(ushort clientID);
    }
}

