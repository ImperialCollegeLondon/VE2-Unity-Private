using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common;
using VE2.Common.TransformWrapper;

namespace VE2.Core.VComponents.API
{
    internal interface IRangedAdjustableInteractionModule : IRangedGrabInteractionModule
    {
        public ITransformWrapper Transform { get; }

        public void ScrollUp();

        public void ScrollDown();
    }
}
