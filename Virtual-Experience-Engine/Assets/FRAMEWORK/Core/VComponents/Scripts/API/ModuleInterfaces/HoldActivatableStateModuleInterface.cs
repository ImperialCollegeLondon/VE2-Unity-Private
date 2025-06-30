using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    internal interface IMultiInteractorActivatableStateModule
    {
        public UnityEvent OnActivate { get; }
        public UnityEvent OnDeactivate { get; }

        public bool IsActivated { get; }

        /// <summary>
        /// When force activated, the activatable will be activated even if no player is holding it. 
        /// </summary>
        /// <param name="toggle"></param>
        public void ToggleAlwaysActivated(bool toggle);
        public IClientIDWrapper MostRecentInteractingClientID { get; }
        public List<IClientIDWrapper> CurrentlyInteractingClientIDs { get; }
    }
}
