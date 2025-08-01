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
        [Title("Ranged Adjustable Interaction Settings", ApplyCondition = true)]
        [SerializeField, PropertyOrder(-100)] private Transform _transformToAdjust = null;
        private ITransformWrapper _transformToAdjustWrapper;
        public ITransformWrapper TransformToAdjust
        {
            get
            {
                if (_transformToAdjustWrapper == null && _transformToAdjust != null)
                    _transformToAdjustWrapper = new TransformWrapper(_transformToAdjust);

                return _transformToAdjustWrapper;
            }
            set => _transformToAdjustWrapper = value; //TODO: Maybe try and also set _transformToAdjust if its castable to Transform?
        }
        [SerializeField, PropertyOrder(-100)] private Transform _adjustableTransform = null;
        private ITransformWrapper _adjustableTransformWrapper;
        public ITransformWrapper AdjustableTransform
        {
            get
            {
                if (_adjustableTransformWrapper == null && _adjustableTransform != null)
                    _adjustableTransformWrapper = new TransformWrapper(_adjustableTransform);

                return _adjustableTransformWrapper;
            }
            set => _adjustableTransformWrapper = value; //TODO: Maybe try and also set _adjustableTransform if its castable to Transform?
        }


        [SerializeField, PropertyOrder(-100)] public bool PointRayTowardsAttachPoint = true;
        [EndGroup, SerializeField, PropertyOrder(-100), ShowIf(nameof(PointRayTowardsAttachPoint), false)] private Transform _rayPointTransformTest = null;
        private ITransformWrapper _rayPointTransformWrapper;
        public ITransformWrapper RayPointTransform
        {
            get
            {
                if (_rayPointTransformWrapper == null && _rayPointTransformTest != null)
                    _rayPointTransformWrapper = new TransformWrapper(_rayPointTransformTest);

                return _rayPointTransformWrapper;
            }
            set => _rayPointTransformWrapper = value; //TODO: Maybe try and also set _rayPointTransform if its castable to Transform?
        }
    }


    internal class RangedAdjustableInteractionModule : RangedGrabInteractionModule, IRangedAdjustableInteractionModule
    {
        public event Action<ushort> OnScrollUp;
        public event Action<ushort> OnScrollDown;

        //This one is ready by the interactor to handle haptics
        public event Action OnValueChanged;

        //TODO - parent class exposes this, likely don't need this here
        public ITransformWrapper AttachPointTransform { get; }

        public ITransformWrapper TransformToPointRayTo { get; }

        public Vector3 WorldSpacePlaneNormal { get; }

        public ITransformWrapper AdjustableTransform { get; }

        public Vector3 LocalAdjustmentAxis { get; }

        public RangedAdjustableInteractionModule(string id, IGrabInteractablesContainer grabInteractablesContainer,
            List<IHandheldInteractionModule> handheldModules, RangedAdjustableInteractionConfig rangedGrabInteractionConfig, GeneralInteractionConfig generalInteractionConfig, SpatialAdjustableServiceConfig spatialAdjustableServiceConfig)
                : base(id, grabInteractablesContainer, handheldModules, rangedGrabInteractionConfig, generalInteractionConfig)
        {
            AttachPointTransform = rangedGrabInteractionConfig.AttachPointWrapper;

            if (rangedGrabInteractionConfig.PointRayTowardsAttachPoint)
                TransformToPointRayTo = AttachPointTransform;
            else
                TransformToPointRayTo = rangedGrabInteractionConfig.RayPointTransform;

            AdjustableTransform = rangedGrabInteractionConfig.AdjustableTransform;

            switch (spatialAdjustableServiceConfig.PlaneNormal)
            {
                case SpatialAdjustmentAxis.XAxis:
                    WorldSpacePlaneNormal = AdjustableTransform.right;
                    break;
                case SpatialAdjustmentAxis.YAxis:
                    WorldSpacePlaneNormal = AdjustableTransform.up;
                    break;
                case SpatialAdjustmentAxis.ZAxis:
                    WorldSpacePlaneNormal = AdjustableTransform.forward;
                    break;
            }

            switch (spatialAdjustableServiceConfig.AdjustmentAxis)
            {
                case SpatialAdjustmentAxis.XAxis:
                    LocalAdjustmentAxis = AdjustableTransform.right;
                    break;
                case SpatialAdjustmentAxis.YAxis:
                    LocalAdjustmentAxis = AdjustableTransform.up;
                    break;
                case SpatialAdjustmentAxis.ZAxis:
                    LocalAdjustmentAxis = AdjustableTransform.forward;
                    break;
            }
        }

        public void ScrollUp(ushort clientID) => OnScrollUp?.Invoke(clientID);
        public void ScrollDown(ushort clientID) => OnScrollDown?.Invoke(clientID);

        public void NotifyValueChanged() => OnValueChanged?.Invoke();
    }
}