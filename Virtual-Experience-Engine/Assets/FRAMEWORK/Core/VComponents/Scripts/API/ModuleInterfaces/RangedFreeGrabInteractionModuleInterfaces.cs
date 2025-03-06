using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VE2.Core.VComponents.API
{
    internal interface IRangedFreeGrabInteractionModule : IRangedGrabInteractionModule
    {
        public void ApplyDeltaWhenGrabbed(Vector3 deltaPostion, Quaternion deltaRotation);
    }

}
