using UnityEngine;
using UnityEngine.Events;

namespace VE2.Core.VComponents.API
{
    public interface IAdjustableStateModule
    {
        public UnityEvent<float> OnValueAdjusted { get; }
        public float Value { get; set; }
        public float MinimumValue {  get; set; }
        public float MaximumValue { get; set; }
        public ushort MostRecentInteractingClientID { get; }
    }
}

