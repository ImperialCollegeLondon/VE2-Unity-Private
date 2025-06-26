
using System;
using UnityEngine;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class RangedClickInteractionConfig : RangedInteractionConfig
    {
        [BeginGroup(Style = GroupStyle.Round, ApplyCondition = true)]
        [Title("Ranged Click Interaction Settings")]
        [SerializeField, EndGroup, PropertyOrder(-100)] public bool ClickAtRangeInVR = true;
    }

    internal class RangedToggleClickInteractionModule : RangedInteractionModule, IRangedToggleClickInteractionModule
    {
        public void ClickDown(InteractorID interactorID)
        {
            //only happens if is valid click
            OnClickDown?.Invoke(interactorID);
        }
        public string ID => IDWrapper.ID;
        public IGameObjectIDWrapper IDWrapper;
        public bool ActivateAtRangeInVR { get; }

        public event Action<InteractorID> OnClickDown;

        public RangedToggleClickInteractionModule(RangedInteractionConfig rangedConfig, GeneralInteractionConfig generalConfig, IGameObjectIDWrapper id, bool activateAtRangeInVR) : base(rangedConfig, generalConfig)
        {
            IDWrapper = id;
            ActivateAtRangeInVR = activateAtRangeInVR;
        }
    }
}