using System;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    internal interface IRangedAdjustableInteractionModule : IRangedGrabInteractionModule
    {
        public ITransformWrapper Transform { get; }

        public void ScrollUp(ushort clientID);

        public void ScrollDown(ushort clientID);

        public event Action OnValueChanged;
    }
}
