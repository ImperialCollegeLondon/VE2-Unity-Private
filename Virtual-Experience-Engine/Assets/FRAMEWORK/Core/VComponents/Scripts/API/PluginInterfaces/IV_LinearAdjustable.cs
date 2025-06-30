using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{   
    public interface IV_LinearAdjustable : IV_GeneralInteractable
    {
        #region State Module Interface
        public UnityEvent<float> OnValueAdjusted { get; }
        public UnityEvent OnGrab { get; }
        public UnityEvent OnDrop { get; }

        public bool IsGrabbed { get; }
        public bool IsLocallyGrabbed { get; }
        public float Value { get; }
        public void SetValue(float value);

        public float MinimumOutputValue { get; set; }
        public float MaximumOutputValue { get; set; }

        public float MinimumSpatialValue { get; set; }
        public float MaximumSpatialValue { get; set; }
        public float SpatialValue { get; set; }
        public int NumberOfValues { get; set; }

        public IClientIDWrapper MostRecentInteractingClientID { get; }
        #endregion

        #region Ranged Interaction Module Interface
        public float InteractRange { get; set; }
        #endregion
    }
}
