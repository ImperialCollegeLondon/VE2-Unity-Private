using System;
using UnityEngine;
using System.Collections.Generic;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class RangedGrabInteractionModule : RangedInteractionModule, IRangedGrabInteractionModule
    {
        internal event Action<InteractorID> OnLocalInteractorRequestGrab;
        internal event Action<InteractorID> OnLocalInteractorRequestDrop;

        public List<IHandheldInteractionModule> HandheldInteractions { get; private set; } = new();
        public bool VrFailsafeGrab { get; private set; } = false;
        public float FailsafeGrabRange { get; private set; }
        public float FailsafeGrabRangeBackOfHand { get; private set; }
        public float FailsafeGrabMultiplier { get; private set; }
        public Vector3 DeltaPosition { get; private set; }
        public Quaternion DeltaRotation { get; private set; }

        public RangedGrabInteractionModule(List<IHandheldInteractionModule> handheldInteractions, GrabInteractionConfig grabInteractionConfig, RangedInteractionConfig config, GeneralInteractionConfig generalInteractionConfig) : base(config, generalInteractionConfig) 
        {
            HandheldInteractions = handheldInteractions;
            FailsafeGrabMultiplier = grabInteractionConfig.failsafeGrabMultiplier;
            VrFailsafeGrab = grabInteractionConfig.VrFailsafeGrab;
            FailsafeGrabRange = grabInteractionConfig.FailsafeGrabRange;
            FailsafeGrabRangeBackOfHand = grabInteractionConfig.FailsafeGrabRangeBackOfHand;
        }

        public void RequestLocalGrab(InteractorID interactorID)
        {
            //Debug.Log("RequestLocalGrab - " + interactorID.InteractorType);
            OnLocalInteractorRequestGrab?.Invoke(interactorID);
        }

        public void RequestLocalDrop(InteractorID interactorID)
        {
            OnLocalInteractorRequestDrop?.Invoke(interactorID);
        }


    }
}