using System;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    internal interface IRangedAdjustableInteractionModule : IRangedGrabInteractionModule
    {
        public ITransformWrapper AttachPointTransform { get; }

        public ITransformWrapper TransformToPointRayTo { get; }

        public void ScrollUp(ushort clientID);

        public void ScrollDown(ushort clientID);

        public event Action OnValueChanged;
    }
}
