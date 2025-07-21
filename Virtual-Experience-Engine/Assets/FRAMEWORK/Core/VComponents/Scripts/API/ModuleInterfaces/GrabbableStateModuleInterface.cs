using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    internal interface IGrabbableStateModule
    {
        public UnityEvent OnGrab { get; }
        public UnityEvent OnDrop { get; }

        public bool IsGrabbed { get; }
        public bool IsLocalGrabbed { get; }
        public IClientIDWrapper MostRecentInteractingClientID { get; }

        #region Force Grab and Drop Interface
        public bool TryLocalGrab(bool lockGrab, VRHandInteractorType priorityHandToGrabWith);

        public void ForceLocalGrab(bool lockGrab, VRHandInteractorType handToGrabWith);
        public void UnlockLocalGrab();

        public void ForceLocalDrop();
        #endregion
    }
}
