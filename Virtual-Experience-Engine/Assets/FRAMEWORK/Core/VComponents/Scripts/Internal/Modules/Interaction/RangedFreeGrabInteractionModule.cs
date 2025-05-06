using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using VE2.Common.API;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.Internal
{
    internal class RangedFreeGrabInteractionModule : RangedGrabInteractionModule, IRangedFreeGrabInteractionModule
    {
        internal event Action<Vector3, Quaternion> OnGrabDeltaApplied;
        
        public RangedFreeGrabInteractionModule(string id, IGrabInteractablesContainer grabInteractablesContainer, ITransformWrapper transform, List<IHandheldInteractionModule> handheldInteractions, 
        GrabInteractionConfig failsafeGrabMultiplier, RangedInteractionConfig config, GeneralInteractionConfig generalInteractionConfig) 
            : base(id, grabInteractablesContainer, transform, handheldInteractions, failsafeGrabMultiplier, config, generalInteractionConfig) { }

        public void ApplyDeltaWhenGrabbed(Vector3 deltaPostion, Quaternion deltaRotation)
        {
            OnGrabDeltaApplied?.Invoke(deltaPostion, deltaRotation);
        }
    }
}
