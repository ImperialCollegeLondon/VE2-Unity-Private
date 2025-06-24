using UnityEngine.Events;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    public interface IV_FreeGrabbable : IV_GeneralInteractable
    {
        #region State Module Interface
        public UnityEvent OnGrab { get; }
        public UnityEvent OnDrop { get; }

        public bool IsGrabbed { get; }
        public IClientIDWrapper MostRecentInteractingClientID { get; }
        #endregion

        #region Ranged Interaction Module Interface
        public float InteractRange { get; set; }
        #endregion
    }
}
