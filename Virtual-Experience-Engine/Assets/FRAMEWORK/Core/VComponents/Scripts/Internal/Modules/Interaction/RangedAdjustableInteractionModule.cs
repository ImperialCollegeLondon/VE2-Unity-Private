using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class RangedAdjustableInteractionModule : RangedGrabInteractionModule, IRangedAdjustableInteractionModule
    {
        public event Action OnScrollUp;
        public event Action OnScrollDown;
        public ITransformWrapper Transform { get; }
        
        public RangedAdjustableInteractionModule(string id, IGrabInteractablesContainer grabInteractablesContainer, ITransformWrapper transform, 
            List<IHandheldInteractionModule> handheldModules, GrabInteractionConfig failsafeGrabMultiplier, RangedInteractionConfig config, 
            GeneralInteractionConfig generalInteractionConfig) : base(id, grabInteractablesContainer, transform, handheldModules, failsafeGrabMultiplier, config, generalInteractionConfig)
        {
            Transform = transform;
        }
        
        public void ScrollUp() => OnScrollUp?.Invoke();
        public void ScrollDown() => OnScrollDown?.Invoke();
    }
}