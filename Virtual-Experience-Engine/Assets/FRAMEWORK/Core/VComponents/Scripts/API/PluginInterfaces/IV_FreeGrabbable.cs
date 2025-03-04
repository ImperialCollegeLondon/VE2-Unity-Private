using UnityEngine.Events;

namespace VE2.Core.VComponents.API
{
    public interface IV_FreeGrabbable 
    {
        #region State Module Interface
        internal IFreeGrabbableStateModule _StateModule { get; }

        public UnityEvent OnGrab => _StateModule.OnGrab;
        public UnityEvent OnDrop => _StateModule.OnDrop;

        public bool IsGrabbed { get { return _StateModule.IsGrabbed; } }
        public ushort MostRecentInteractingClientID => _StateModule.MostRecentInteractingClientID;
        #endregion

        #region Ranged Interaction Module Interface
        internal IRangedGrabInteractionModule _RangedGrabModule{ get; }
        public float InteractRange { get => _RangedGrabModule.InteractRange; set => _RangedGrabModule.InteractRange = value; }
        #endregion

        #region General Interaction Module Interface
        //We have two General Interaction Modules here, it doesn't matter which one we point to, both share the same General Interaction Config object!
        public bool AdminOnly {get => _RangedGrabModule.AdminOnly; set => _RangedGrabModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _RangedGrabModule.EnableControllerVibrations; set => _RangedGrabModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _RangedGrabModule.ShowTooltipsAndHighlight; set => _RangedGrabModule.ShowTooltipsAndHighlight = value; }
        #endregion
    }
}