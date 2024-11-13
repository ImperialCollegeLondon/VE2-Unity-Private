using System;
using System.Collections;
using UnityEngine;
using System.Diagnostics;
using VE2.Core.VComponents.InteractableInterfaces;
using System.Collections.Generic;
using VE2.Common;

namespace VE2.Core.VComponents.Internal
{
    internal class RangedGrabInteractionModule : RangedInteractionModule, IRangedGrabInteractionModule
    {
        internal event Action<InteractorID> OnLocalInteractorRequestGrab;
        internal event Action<InteractorID> OnLocalInteractorRequestDrop;
        public List<IHandheldInteraction> HandheldInteractions {get; private set; } = new();

        public Transform CurrentGrabbingGrabberTransform { get; private set; }

        public RangedGrabInteractionModule(List<IHandheldInteraction> handheldInteractions, RangedInteractionConfig config, GeneralInteractionConfig generalInteractionConfig) : base(config, generalInteractionConfig) 
        {
            HandheldInteractions = handheldInteractions;
        }

        public void RequestLocalGrab(InteractorID interactorID)
        {
            OnLocalInteractorRequestGrab?.Invoke(interactorID);
        }

        public void RequestLocalDrop(InteractorID interactorID)
        {
            OnLocalInteractorRequestDrop?.Invoke(interactorID);
        }

        public void ConfirmGrabOnInteractor(IInteractor interactor)
        {
            CurrentGrabbingGrabberTransform = interactor.ConfirmGrab(this);
        }

        public void ConfirmDropOnInteractor(IInteractor interactor)
        {
            CurrentGrabbingGrabberTransform = null;
            interactor.ConfirmDrop();
        }
    }
}