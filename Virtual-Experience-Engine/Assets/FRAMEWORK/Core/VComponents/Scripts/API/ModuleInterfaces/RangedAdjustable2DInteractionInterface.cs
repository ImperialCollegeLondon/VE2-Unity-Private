using System;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    internal interface IRangedAdjustable2DInteractionModule : IRangedAdjustableInteractionModule
    {
        public void Scroll(ushort clientID);
    }
}
