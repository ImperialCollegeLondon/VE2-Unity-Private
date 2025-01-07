using UnityEngine;
using UnityEngine.Events;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.NonInteractableInterfaces;

namespace VE2.Core.VComponents.PluginInterfaces
{   
     public interface IV_LinearAdjustable
    {
        #region State Module Interface
        protected IAdjustableStateModule _AdjustableStateModule { get; }
        protected IFreeGrabbableStateModule _FreeGrabbableStateModule { get; }

        public UnityEvent<float> OnValueAdjusted => _AdjustableStateModule.OnValueAdjusted;
        public UnityEvent OnGrab => _FreeGrabbableStateModule.OnGrab;
        public UnityEvent OnDrop => _FreeGrabbableStateModule.OnDrop;

        public bool IsGrabbed { get { return _FreeGrabbableStateModule.IsGrabbed; } }
        public float Value { get { return _AdjustableStateModule.Value; } set { _AdjustableStateModule.Value = value; } }
        public float MinimumValue { get => _AdjustableStateModule.MinimumValue; set => _AdjustableStateModule.MinimumValue = value; }
        public float MaximumValue { get => _AdjustableStateModule.MaximumValue; set => _AdjustableStateModule.MaximumValue = value; }

        public float MinimumOutputValue { get; set; }
        public float MaximumOutputValue { get; set; }
        public float OutputValue { get; set; }
        public int NumberOfValues { get; set; }

        public ushort MostRecentInteractingClientID => _FreeGrabbableStateModule.MostRecentInteractingClientID;
        #endregion

        #region Ranged Interaction Module Interface
        protected IRangedAdjustableInteractionModule _RangedAdjustableModule{ get; }
        public float InteractRange { get => _RangedAdjustableModule.InteractRange; set => _RangedAdjustableModule.InteractRange = value; }
        #endregion

        #region General Interaction Module Interface
        //We have two General Interaction Modules here, it doesn't matter which one we point to, both share the same General Interaction Config object!
        public bool AdminOnly {get => _RangedAdjustableModule.AdminOnly; set => _RangedAdjustableModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _RangedAdjustableModule.EnableControllerVibrations; set => _RangedAdjustableModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _RangedAdjustableModule.ShowTooltipsAndHighlight; set => _RangedAdjustableModule.ShowTooltipsAndHighlight = value; }
        #endregion
    }
}

