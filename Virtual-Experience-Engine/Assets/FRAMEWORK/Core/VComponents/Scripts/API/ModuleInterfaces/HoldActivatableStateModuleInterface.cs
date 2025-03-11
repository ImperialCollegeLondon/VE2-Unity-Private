using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VE2.Core.VComponents.API
{
    internal interface IMultiInteractorActivatableStateModule
    {
        public UnityEvent OnActivate { get; }
        public UnityEvent OnDeactivate { get; }

        public bool IsActivated { get; }
        public ushort MostRecentInteractingClientID { get; }
        public List<ushort> CurrentlyInteractingClientIDs { get; }
    }
}