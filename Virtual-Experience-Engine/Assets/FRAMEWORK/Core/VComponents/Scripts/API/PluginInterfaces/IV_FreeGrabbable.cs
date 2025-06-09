using UnityEngine.Events;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    public interface IV_FreeGrabbable 
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

        #region General Interaction Module Interface
        //We have two General Interaction Modules here, it doesn't matter which one we point to, both share the same General Interaction Config object!
        public bool AdminOnly { get; set; }
        public bool EnableControllerVibrations { get; set; }
        public bool ShowTooltipsAndHighlight { get; set; }
        #endregion

        #region Force Grab and Drop Interface
        public bool TryLocalGrab(bool lockGrab, VRHandInteractorType priorityHandToGrabWith = VRHandInteractorType.RightHandVR);

        public void ForceLocalGrab (bool lockGrab, VRHandInteractorType handToGrabWith = VRHandInteractorType.RightHandVR);
        public void UnlockLocalGrab();
        public void ForceLocalDrop();

        #endregion

    }
}
