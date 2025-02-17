using System;
using UnityEngine;
using VE2.Common;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.Common
{
    [Serializable]
    public class BaseWorldStateConfig 
    {
        [BeginGroup(Style = GroupStyle.Round, ApplyCondition = true)]
        [Title("Transmission Settings", ApplyCondition = true)]
        //[HideIf(nameof(MultiplayerSupportPresent), false)]
        [SerializeField] public bool IsNetworked = true;

        //[HideIf(nameof(MultiplayerSupportPresent), false)]
        [DisableIf(nameof(IsNetworked), false)]
        [EndGroup(ApplyCondition = true, Order = 5)]
        [SpaceArea(spaceAfter: 10, Order = -1), SerializeField, IgnoreParent] public RepeatedTransmissionConfig RepeatedTransmissionConfig = new();
    }
}

