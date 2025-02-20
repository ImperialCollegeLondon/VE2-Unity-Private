using UnityEngine;

namespace VE2.Core.VComponents.API
{
    public interface IHandheldScrollInteractionModule: IHandheldInteractionModule
    {
        public void ScrollUp(ushort clientID);
        public void ScrollDown(ushort clientID);
    }
}

