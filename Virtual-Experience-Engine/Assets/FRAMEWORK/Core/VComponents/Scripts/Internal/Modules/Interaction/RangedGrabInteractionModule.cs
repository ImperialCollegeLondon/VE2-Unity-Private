using System;
using UnityEngine;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using VE2.Common.API;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class RangedGrabInteractionConfig : RangedInteractionConfig
    {
        [Title("Ranged Grab Interaction Settings", ApplyCondition = true)]
        [BeginGroup(Style = GroupStyle.Round)]

        //Ideally this would be private, but the custom property drawer can't see the PropertyOrder value if it is. 
        [SerializeField, PropertyOrder(-10)] public Transform _attachPoint = null; 
        private ITransformWrapper _attachPointWrapper;
        public ITransformWrapper AttachPointWrapper
        {
            get
            {
                if (_attachPointWrapper == null && _attachPoint != null)
                    _attachPointWrapper = new TransformWrapper(_attachPoint);

                return _attachPointWrapper;
            }
            set => _attachPointWrapper = value; //TODO: Maybe try and also set _attachPoint if its castable to Transform?
        }

        [SerializeField, PropertyOrder(-9)] public bool VRRaySnap = true;
        [SerializeField, PropertyOrder(-8), ShowIf(nameof(VRFailsafeGrab), true)] public float VRRaySnapRangeFrontOfHand = 0.15f;
        [SerializeField, PropertyOrder(-7), ShowIf(nameof(VRFailsafeGrab), true)] public float VRRaySnapRangeBackOfHand = 0.1f;
        [SerializeField, PropertyOrder(-6)] public bool VRFailsafeGrab = true;
        [EndGroup]
        [SerializeField, PropertyOrder(-5), ShowIf(nameof(VRFailsafeGrab), true), Range(1f, 2f)] public float FailsafeGrabMultiplier = 1.2f;

        //TODO - VR raysnap should be allowed even if failsafe grab is disabled
        //Maybe we want a separate toggle to define whether we also allow ray snapping?
        //But failsafe grab multiplier is a mult on the VR raySnapRangeFrontOfHand? Maybe it should just be a separate float range 
    }

    internal class RangedGrabInteractionModule : RangedInteractionModule, IRangedGrabInteractionModule
    {
        internal event Action<InteractorID> OnLocalInteractorRequestGrab;
        internal event Action<InteractorID> OnLocalInteractorRequestDrop;

        public List<IHandheldInteractionModule> HandheldInteractions { get; private set; } = new();

        public Vector3 DeltaPosition { get; private set; }
        public Quaternion DeltaRotation { get; private set; }

        public ITransformWrapper AttachPoint => _rangedGrabInteractionConfig.AttachPointWrapper;
        public bool VrRaySnap => _rangedGrabInteractionConfig.VRRaySnap;
        public float VRRaySnapRange => _rangedGrabInteractionConfig.VRRaySnapRangeFrontOfHand;
        public float VRRaySnapRangeBackOfHand => _rangedGrabInteractionConfig.VRRaySnapRangeBackOfHand;
        public float FailsafeGrabMultiplier => _rangedGrabInteractionConfig.FailsafeGrabMultiplier;

        private readonly string _id;
        private readonly IGrabInteractablesContainer _grabInteractablesContainer;
        private readonly RangedGrabInteractionConfig _rangedGrabInteractionConfig;

        //TODO: Figure out the attach point, don't really want to inject it as a separate param if it's already in the config...

        public RangedGrabInteractionModule(string id, IGrabInteractablesContainer grabInteractablesContainer, List<IHandheldInteractionModule> handheldInteractions,
            RangedGrabInteractionConfig grabInteractionConfig, GeneralInteractionConfig generalInteractionConfig) : base(grabInteractionConfig, generalInteractionConfig)
        {
            _id = id;
            HandheldInteractions = handheldInteractions;
            _grabInteractablesContainer = grabInteractablesContainer;
            _grabInteractablesContainer.RegisterGrabInteractable(this, id);
            _rangedGrabInteractionConfig = grabInteractionConfig;
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