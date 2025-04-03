using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common.TransformWrapper;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class RangedAdjustableInteractionModule : RangedGrabInteractionModule, IRangedAdjustableInteractionModule
    {
        public event Action OnScrollUp;
        public event Action OnScrollDown;
        public ITransformWrapper Transform { get; }
        
        public RangedAdjustableInteractionModule(ITransformWrapper transform, List<IHandheldInteractionModule> handheldModules, RangedInteractionConfig config, GeneralInteractionConfig generalInteractionConfig) : base(handheldModules, config, generalInteractionConfig)
        {
            Transform = transform;
        }
        
        public void ScrollUp() => OnScrollUp?.Invoke();
        public void ScrollDown() => OnScrollDown?.Invoke();
    }
}