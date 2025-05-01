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
        internal event Action<Transform> OnInspectModeEnter;
        internal event Action<Transform> OnInspectModeExit;
        public List<IHandheldInteractionModule> HandheldInteractions {get; private set; } = new();

        public Vector3 DeltaPosition { get; private set; }
        public Quaternion DeltaRotation { get; private set; }
        public RangedGrabInteractionModule(List<IHandheldInteractionModule> handheldInteractions, RangedInteractionConfig config, GeneralInteractionConfig generalInteractionConfig) : base(config, generalInteractionConfig) 
        {
            HandheldInteractions = handheldInteractions;
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

        public void SetInspectModeEnter(Transform grabberTransform)
        {
            OnInspectModeEnter?.Invoke(grabberTransform);
        }

        public void SetInspectModeExit(Transform grabberTransform)
        {
            OnInspectModeExit?.Invoke(grabberTransform);
        }
    }
}