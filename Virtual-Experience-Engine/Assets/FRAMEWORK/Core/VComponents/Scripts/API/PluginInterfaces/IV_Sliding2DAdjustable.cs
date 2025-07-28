using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    public interface IV_Sliding2DAdjustable : IV_GeneralInteractable
    {
        #region State Module Interface
        public UnityEvent<Vector2> OnValueAdjusted { get; }
        public UnityEvent OnGrab { get; }
        public UnityEvent OnDrop { get; }

        public bool IsGrabbed { get; }
        public bool IsLocallyGrabbed { get; }
        public Vector2 Value { get; }
        public void SetValue(Vector2 value);

        public Vector2 MinimumOutputValue { get; set; }
        public Vector2 MaximumOutputValue { get; set; }

        public Vector2 MinimumSpatialValue { get; set; }
        public Vector2 MaximumSpatialValue { get; set; }
        public Vector2 SpatialValue { get; set; }
        public int NumberOfValues { get; set; }

        public IClientIDWrapper MostRecentGrabbingClientID { get; }
        public IClientIDWrapper MostRecentAdjustingClientID { get; }
        #endregion

        #region Ranged Interaction Module Interface
        public float InteractRange { get; set; }
        #endregion
    }
}