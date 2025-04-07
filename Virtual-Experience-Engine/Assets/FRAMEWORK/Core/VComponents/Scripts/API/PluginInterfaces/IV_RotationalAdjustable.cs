using UnityEngine;
using UnityEngine.Events;

namespace VE2.Core.VComponents.API
{   
     public interface IV_RotationalAdjustable
    {
        #region State Module Interface
        internal IAdjustableStateModule _AdjustableStateModule { get; }
        internal IGrabbableStateModule _GrabbableStateModule { get; }

        public UnityEvent<float> OnValueAdjusted => _AdjustableStateModule.OnValueAdjusted;
        public UnityEvent OnGrab => _GrabbableStateModule.OnGrab;
        public UnityEvent OnDrop => _GrabbableStateModule.OnDrop;

        public bool IsGrabbed { get { return _GrabbableStateModule.IsGrabbed; } }
        public bool IsLocallyGrabbed { get { return _GrabbableStateModule.IsLocalGrabbed; } }
        public float Value => _AdjustableStateModule.OutputValue;
        public void SetValue(float value) => _AdjustableStateModule.SetOutputValue(value);
        public float MinimumOutputValue { get => _AdjustableStateModule.MinimumOutputValue; set => _AdjustableStateModule.MinimumOutputValue = value; }
        public float MaximumOutputValue { get => _AdjustableStateModule.MaximumOutputValue; set => _AdjustableStateModule.MaximumOutputValue = value; }

        public float MinimumSpatialValue { get; set; }
        public float MaximumSpatialValue { get; set; }
        public float SpatialValue { get; set; }
        public int NumberOfValues { get; set; }

        public void SetMinimumAndMaximumSpatialValuesRange(float min, float max)
        {
            MinimumSpatialValue = min;
            MaximumSpatialValue = max;
        }

        public void SetMinimumAndMaximumOutputValuesRange(float min, float max)
        {
            MinimumOutputValue = min;
            MaximumOutputValue = max;
        }
        
        public ushort MostRecentInteractingClientID => _GrabbableStateModule.MostRecentInteractingClientID;
        #endregion

        #region Ranged Interaction Module Interface
        internal IRangedAdjustableInteractionModule _RangedAdjustableModule{ get; }
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

