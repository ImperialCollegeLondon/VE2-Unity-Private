using System.Collections.Generic;
using UnityEngine;
using VE2.Common;
using VE2.Common.TransformWrapper;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.Internal;

namespace VE2.Core.VComponents.Internal
{
    internal class RangedAdjustableInteractionModule : RangedGrabInteractionModule, IRangedAdjustableInteractionModule
    {
        public ITransformWrapper Transform { get; }
        
        public RangedAdjustableInteractionModule(ITransformWrapper transform, List<IHandheldInteractionModule> handheldModules, RangedInteractionConfig config, GeneralInteractionConfig generalInteractionConfig) : base(handheldModules, config, generalInteractionConfig)
        {
            Transform = transform;
        }
    }
}