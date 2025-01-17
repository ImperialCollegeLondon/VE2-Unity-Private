using System;
using UnityEngine;


namespace VE2.Core.VComponents.InternalInterfaces {
    public interface IGrabbableRigidbody
    {

        public event Action<ushort> InternalOnGrab;

        public event Action<ushort> InternalOnDrop;

    }

}
