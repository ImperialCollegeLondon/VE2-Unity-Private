using System.Collections.Generic;
using UnityEngine.Events;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    public interface IV_HoldActivatable : IV_GeneralInteractable
    {
        #region State Module Interface
        public UnityEvent OnActivate { get; }
        public UnityEvent OnDeactivate { get; }

        public bool IsActivated { get; }
        public IClientIDWrapper MostRecentInteractingClientID { get; }
        public List<IClientIDWrapper> CurrentlyInteractingClientIDs { get; }
        #endregion

        #region Ranged Interaction Module Interface
        public float InteractRange { get; set; }
        #endregion
    }
}
