using System;
using UnityEngine;

namespace VE2.Core.VComponents.API 
{
    internal interface IGrabbableRigidbody
    {
        public event Action<ushort> InternalOnGrab;
        public event Action<ushort> InternalOnDrop;

        public bool FreeGrabbableHandlesKinematics { get; set; }
    }
}
