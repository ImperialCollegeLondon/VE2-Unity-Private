using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VE2.Core.VComponents.NonInteractableInterfaces
{
    public interface IFreeGrabbableStateModule
    {
        public UnityEvent OnGrab { get; }
        public UnityEvent OnDrop { get; }

        public bool IsGrabbed { get; }
        public bool IsLocalGrabbed { get; }
        public ushort MostRecentInteractingClientID { get; }
    }
}
