using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.API
{
    internal interface IAdjustable2DStateModule
    {
        public UnityEvent<Vector2> OnValueAdjusted { get; }

        public Vector2 OutputValue { get; }
        public void SetOutputValue(Vector2 value);

        public Vector2 MinimumOutputValue { get; set; }
        public Vector2 MaximumOutputValue { get; set; }

        public IClientIDWrapper MostRecentInteractingClientID { get; }
    }
}