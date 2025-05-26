using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class RangedAdjustableInteractionConfig : RangedGrabInteractionConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Adjustable Interaction Settings", ApplyCondition = true)]
        [EndGroup]
        [SerializeField, PropertyOrder(-100)] public Transform TransformToAdjust = null;
    }

    internal class RangedAdjustableInteractionModule : RangedGrabInteractionModule, IRangedAdjustableInteractionModule
    {
        public event Action OnScrollUp;
        public event Action OnScrollDown;
        public ITransformWrapper Transform { get; }

        public RangedAdjustableInteractionModule(string id, IGrabInteractablesContainer grabInteractablesContainer, ITransformWrapper transform,
            List<IHandheldInteractionModule> handheldModules, RangedGrabInteractionConfig rangedGrabInteractionConfig, GeneralInteractionConfig generalInteractionConfig) 
                : base(id, grabInteractablesContainer, transform, handheldModules, rangedGrabInteractionConfig, generalInteractionConfig)
        {
            Transform = transform;
        }

        public void ScrollUp() => OnScrollUp?.Invoke();
        public void ScrollDown() => OnScrollDown?.Invoke();
    }
}