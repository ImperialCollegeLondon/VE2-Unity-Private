using System.Collections;
using System.Collections.Generic;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    internal interface IRangedGrabInteractionModule : IRangedInteractionModule
    {
        public void RequestLocalGrab(InteractorID interactorID);
        public void RequestLocalDrop(InteractorID interactorID);
        public List<IHandheldInteractionModule> HandheldInteractions { get; }
        public ITransformWrapper AttachPoint { get; }
        public bool VrRaySnap { get; }
        public float VRRaySnapRange { get; }
        public float VRRaySnapRangeBackOfHand { get; }
        public float FailsafeGrabMultiplier { get; }
    }
}