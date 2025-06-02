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
    }
}
