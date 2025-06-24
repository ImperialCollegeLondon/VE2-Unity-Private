using UnityEngine.Events;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    public interface IV_HandheldActivatable : IV_GeneralInteractable
    {
        #region State Module Interface

        public UnityEvent OnActivate { get; }
        public UnityEvent OnDeactivate { get; }

        public bool IsActivated { get; }
        public void Activate();
        public void Deactivate();
        public void SetActivated(bool isActivated);
        public IClientIDWrapper MostRecentInteractingClientID { get; }

        #endregion
    }
}
