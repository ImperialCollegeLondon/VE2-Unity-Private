using System;
using UnityEngine;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using VE2.Common.API;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.Internal
{
    internal class RangedGrabInteractionModule : RangedInteractionModule, IRangedGrabInteractionModule
    {
        internal event Action<InteractorID> OnLocalInteractorRequestGrab;
        internal event Action<InteractorID> OnLocalInteractorRequestDrop;

        public List<IHandheldInteractionModule> HandheldInteractions { get; private set; } = new();
        public ITransformWrapper AttachPoint { get; private set; } = null;
        public bool VrRaySnap { get; private set; } = false;
        public float VRRaySnapRange { get; private set; }
        public float VRRaySnapRangeBackOfHand { get; private set; }
        public float FailsafeGrabMultiplier { get; private set; }
        public Vector3 DeltaPosition { get; private set; }
        public Quaternion DeltaRotation { get; private set; }

        private readonly string _id;
        private readonly IGrabInteractablesContainer _grabInteractablesContainer;

        public RangedGrabInteractionModule(string id, IGrabInteractablesContainer grabInteractablesContainer, ITransformWrapper attachPoint, List<IHandheldInteractionModule> handheldInteractions, 
            GrabInteractionConfig grabInteractionConfig, RangedInteractionConfig config, GeneralInteractionConfig generalInteractionConfig) : base(config, generalInteractionConfig) 
        {
            _id = id;
            _grabInteractablesContainer = grabInteractablesContainer;
            _grabInteractablesContainer.RegisterGrabInteractable(this, id);

            AttachPoint = attachPoint;
            HandheldInteractions = handheldInteractions;
            FailsafeGrabMultiplier = grabInteractionConfig.failsafeGrabMultiplier;
            VrRaySnap = grabInteractionConfig.VrFailsafeGrab;
            VRRaySnapRange = grabInteractionConfig.VRRaySnapRange;
            VRRaySnapRangeBackOfHand = grabInteractionConfig.VRRaySnapRangeBackOfHand;
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

        public void TearDown()
        {
            _grabInteractablesContainer.DeregisterGrabInteractable(_id);
        }
    }
}