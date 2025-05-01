using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common.TransformWrapper;

namespace VE2.Core.VComponents.API
{
    internal interface IRangedGrabInteractionModule : IRangedInteractionModule
    {
        public void RequestLocalGrab(InteractorID interactorID);
        public void RequestLocalDrop(InteractorID interactorID);
        public List<IHandheldInteractionModule> HandheldInteractions { get; }
        public ITransformWrapper AttachPoint { get; }
        public bool VrFailsafeGrab { get; }
        public float FailsafeGrabRange { get; }
        public float FailsafeGrabRangeBackOfHand { get; }
        public float FailsafeGrabMultiplier { get; }
    }
}