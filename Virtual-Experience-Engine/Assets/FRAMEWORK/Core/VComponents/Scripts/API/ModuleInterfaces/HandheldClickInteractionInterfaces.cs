using System;

namespace VE2.Core.VComponents.API
{
    public interface IHandheldClickInteractionModule : IHandheldInteractionModule
    {
        public void Click(ushort clientID);
    }
}

