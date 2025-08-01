using System;
using VE2.Common.Shared;
using UnityEngine;

namespace VE2.Core.VComponents.API
{
    internal interface IRangedAdjustableInteractionModule : IRangedGrabInteractionModule
    {
        public ITransformWrapper AttachPointTransform { get; }

        public ITransformWrapper TransformToPointRayTo { get; }

        public ITransformWrapper AdjustableTransform { get; }

        public Vector3 WorldSpacePlaneNormal { get; }

        public Vector3 LocalAdjustmentAxis { get; }

        public void ScrollUp(ushort clientID);

        public void ScrollDown(ushort clientID);

        public event Action OnValueChanged;
    }
}
