using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    public interface IV_HandheldAdjustable
    {
        #region State Module Interface
        public UnityEvent<float> OnValueAdjusted { get; }
        public float Value { get; }
        public void SetValue(float value);
        public float MinimumValue { get; set; }
        public float MaximumValue { get; set; }
        public IClientIDWrapper MostRecentInteractingClientID { get; }
        #endregion

        #region General Interaction Module Interface
        public bool AdminOnly { get; set; }
        public bool EnableControllerVibrations { get; set; }
        public bool ShowTooltipsAndHighlight { get; set; }
        #endregion
    }
}
