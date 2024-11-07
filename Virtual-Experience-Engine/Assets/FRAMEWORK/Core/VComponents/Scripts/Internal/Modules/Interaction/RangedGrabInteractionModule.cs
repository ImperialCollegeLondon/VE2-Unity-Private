using System;
using System.Collections;
using UnityEngine;
using System.Diagnostics;
using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.Core.VComponents.Internal
{
    internal class RangedGrabInteractionModule : RangedInteractionModule, IRangedGrabInteractionModule
    {
        internal event Action<InteractorID> OnLocalInteractorGrab;
        internal event Action<InteractorID> OnLocalInteractorDrop;

        public RangedGrabInteractionModule(RangedInteractionConfig config, GeneralInteractionConfig generalInteractionConfig) : base(config, generalInteractionConfig) { }

        public void LocalInteractorGrab(InteractorID interactorID)
        {
            OnLocalInteractorGrab?.Invoke(interactorID);
        }

        public void LocalInteractorDrop(InteractorID interactorID)
        {
            OnLocalInteractorDrop?.Invoke(interactorID);
        }
    }
}