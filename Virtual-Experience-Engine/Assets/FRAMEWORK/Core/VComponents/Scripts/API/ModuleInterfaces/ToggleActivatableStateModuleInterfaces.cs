using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VE2.Core.VComponents.API
{
    internal interface ISingleInteractorActivatableStateModule
    {
        public UnityEvent OnActivate { get; }
        public UnityEvent OnDeactivate { get; }

        public bool IsActivated { get; set; }
        public ushort MostRecentInteractingClientID { get; }
    }
}
