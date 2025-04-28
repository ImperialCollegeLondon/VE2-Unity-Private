using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class RangedFreeGrabInteractionModule : RangedGrabInteractionModule, IRangedFreeGrabInteractionModule
    {
        internal event Action<Vector3, Quaternion> OnGrabDeltaApplied;
        internal event Action<bool> OnInspectModeSet;
        public RangedFreeGrabInteractionModule(List<IHandheldInteractionModule> handheldInteractions, RangedInteractionConfig config, GeneralInteractionConfig generalInteractionConfig) : base(handheldInteractions, config, generalInteractionConfig) { }

        public void ApplyDeltaWhenGrabbed(Vector3 deltaPostion, Quaternion deltaRotation)
        {
            OnGrabDeltaApplied?.Invoke(deltaPostion, deltaRotation);
        }

        public void SetInspectModeWhenGrabbed(bool isInspectMode)
        {
            OnInspectModeSet?.Invoke(isInspectMode);
        }
    }
}
