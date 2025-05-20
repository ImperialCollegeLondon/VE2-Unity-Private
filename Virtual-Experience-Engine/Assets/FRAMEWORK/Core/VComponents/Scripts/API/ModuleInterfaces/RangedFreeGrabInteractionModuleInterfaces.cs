using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VE2.Core.VComponents.API
{
    internal interface IRangedFreeGrabInteractionModule : IRangedGrabInteractionModule
    {
        public bool KeepInspectModeOrientation { get; set; }
        public bool UseAttachPointOrientationOnGrab { get; set; }

        public void SetInspectModeEnter();
        public void SetInspectModeExit();
        public void ApplyDeltaWhenGrabbed(Vector3 deltaPostion, Quaternion deltaRotation);
    }

}
