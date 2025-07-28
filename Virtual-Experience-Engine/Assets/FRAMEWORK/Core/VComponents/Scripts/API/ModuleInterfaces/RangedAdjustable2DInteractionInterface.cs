using System;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    internal interface IRangedAdjustable2DInteractionModule : IRangedAdjustableInteractionModule
    {
        public void ScrollLeft(ushort clientID);
        public void ScrollRight(ushort clientID);
    }
}
