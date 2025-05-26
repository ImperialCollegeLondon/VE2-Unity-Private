using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VE2.Core.VComponents.API
{
    internal interface IRangedFreeGrabInteractionModule : IRangedGrabInteractionModule
    {
        public bool PreserveInspectModeOrientation { get; set; }
        public bool AlignOrientationOnGrab { get; set; }

        public void NotifyInspectModeEnter();
        public void SetInspectModeExit();
        public void ApplyDeltaWhenGrabbed(Vector3 deltaPostion, Quaternion deltaRotation);
    }

}
