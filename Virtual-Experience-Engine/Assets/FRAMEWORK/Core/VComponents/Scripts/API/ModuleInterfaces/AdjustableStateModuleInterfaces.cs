using UnityEngine;
using UnityEngine.Events;

namespace VE2.Core.VComponents.API
{
    internal interface IAdjustableStateModule
    {
        public UnityEvent<float> OnValueAdjusted { get; }

        public float OutputValue { get; }
        public void SetOutputValue(float value);

        public float MinimumOutputValue {  get; set; }
        public float MaximumOutputValue { get; set; }
        
        public ushort MostRecentInteractingClientID { get; }
    }
}

