using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common;
using VE2.Common.TransformWrapper;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.NonInteractableInterfaces;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    public class RotationalAdjustableConfig
    {
        [SerializeField, IgnoreParent] public SpatialAdjustableServiceConfig RotationalAdjustableServiceConfig = new();
        [SerializeField, IgnoreParent] public AdjustableStateConfig AdjustableStateConfig = new();
        [SerializeField, IgnoreParent] public FreeGrabbableStateConfig GrabbableStateConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public RangedInteractionConfig RangedInteractionConfig = new();
        [SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
    }

    public class RotationalAdjustableService
    {
        private float _spatialValue;
        public float SpatialValue { get => _spatialValue; set => SetSpatialValue(value); }
        private float _minimumSpatialValue, _maximumSpatialValue;
        public float MinimumSpatialValue { get => _minimumSpatialValue; set => _minimumSpatialValue = value; }
        public float MaximumSpatialValue { get => _maximumSpatialValue; set => _maximumSpatialValue = value; }
        private int _numberOfValues;
        public int NumberOfValues { get => _numberOfValues; set => UpdateSteps(value); }

        #region Interfaces
        public IAdjustableStateModule AdjustableStateModule => _AdjustableStateModule;
        public IFreeGrabbableStateModule FreeGrabbableStateModule => _FreeGrabbableStateModule;
        public IRangedAdjustableInteractionModule RangedAdjustableInteractionModule => _RangedAdjustableInteractionModule;
        #endregion

        #region Modules
        private readonly AdjustableStateModule _AdjustableStateModule;
        private readonly FreeGrabbableStateModule _FreeGrabbableStateModule;
        private readonly RangedAdjustableInteractionModule _RangedAdjustableInteractionModule;
        #endregion

        private readonly SpatialAdjustmentProperty _adjustmentProperty;
        private readonly ITransformWrapper _transformWrapper;
        private readonly SpatialAdjustmentType _adjustmentType;
        private float _oldRotationalValue;
        private int numberOfRevolutions = 0;

        public RotationalAdjustableService(ITransformWrapper transformWrapper, List<IHandheldInteractionModule> handheldInteractions, RotationalAdjustableConfig config, VE2Serializable adjustableState, VE2Serializable grabbableState, string id,
            WorldStateModulesContainer worldStateModulesContainer, InteractorContainer interactorContainer)
        {
            ITransformWrapper attachPointTransform = config.GrabbableStateConfig.AttachPoint == null ? transformWrapper : new TransformWrapper(config.GrabbableStateConfig.AttachPoint);

            _RangedAdjustableInteractionModule = new(attachPointTransform, handheldInteractions, config.RangedInteractionConfig, config.GeneralInteractionConfig);

            _transformWrapper = transformWrapper;

            _adjustmentType = config.RotationalAdjustableServiceConfig.AdjustmentType;
            _adjustmentProperty = config.RotationalAdjustableServiceConfig.AdjustmentProperty;

            if (_adjustmentProperty == SpatialAdjustmentProperty.Discrete)
                _numberOfValues = config.RotationalAdjustableServiceConfig.NumberOfValues;

            _minimumSpatialValue = config.RotationalAdjustableServiceConfig.MinimumSpatialValue;
            _maximumSpatialValue = config.RotationalAdjustableServiceConfig.MaximumSpatialValue;

            _AdjustableStateModule = new(adjustableState, config.AdjustableStateConfig, $"ADJ-{id}", worldStateModulesContainer);
            _FreeGrabbableStateModule = new(grabbableState, config.GrabbableStateConfig, $"FG-{id}", worldStateModulesContainer, interactorContainer, RangedAdjustableInteractionModule);

            _RangedAdjustableInteractionModule.OnLocalInteractorRequestGrab += (InteractorID interactorID) => _FreeGrabbableStateModule.SetGrabbed(interactorID);
            _RangedAdjustableInteractionModule.OnLocalInteractorRequestDrop += (InteractorID interactorID) => _FreeGrabbableStateModule.SetDropped(interactorID);

            _FreeGrabbableStateModule.OnGrabConfirmed += OnGrabConfirmed;
            _FreeGrabbableStateModule.OnDropConfirmed += OnDropConfirmed;

            _AdjustableStateModule.OnValueChangedInternal += (float value) => OnStateValueChanged(value);

            SetValueOnStateModule(config.AdjustableStateConfig.StartingOutputValue);
            _oldRotationalValue = ConvertToSpatialValue(config.AdjustableStateConfig.StartingOutputValue);
        }

        private void OnGrabConfirmed()
        {
            //_initialGrabberPosition = _FreeGrabbableStateModule.CurrentGrabbingInteractor.GrabberTransform.position;
        }

        private void OnDropConfirmed()
        {

        }

        private void SetSpatialValue(float spatialValue)
        {
            _spatialValue = Mathf.Clamp(spatialValue, _minimumSpatialValue, _maximumSpatialValue);
            float OutputValue = ConvertToOutputValue(_spatialValue);
            SetValueOnStateModule(OutputValue);
        }

        private void OnStateValueChanged(float value)
        {
            _spatialValue = ConvertToSpatialValue(value);

            switch (_adjustmentType)
            {
                case SpatialAdjustmentType.XAxis:
                    _transformWrapper.localRotation = Quaternion.Euler(_spatialValue, _transformWrapper.localRotation.y, _transformWrapper.localRotation.z);
                    break;
                case SpatialAdjustmentType.YAxis:
                    _transformWrapper.localRotation = Quaternion.Euler(_transformWrapper.localRotation.x, _spatialValue, _transformWrapper.localRotation.z);
                    break;
                case SpatialAdjustmentType.ZAxis:
                    _transformWrapper.localRotation = Quaternion.Euler(_transformWrapper.localRotation.x, _transformWrapper.localRotation.y, _spatialValue);
                    break;
            }
        }

        public void HandleFixedUpdate()
        {
            _FreeGrabbableStateModule.HandleFixedUpdate();
            if (_FreeGrabbableStateModule.IsLocalGrabbed)
            {
                TrackPosition(_FreeGrabbableStateModule.CurrentGrabbingInteractor.GrabberTransform.position);
            }

            _AdjustableStateModule.HandleFixedUpdate();
        }

        private void TrackPosition(Vector3 grabberPosition)
        {
            Vector3 directionToGrabber = grabberPosition - _transformWrapper.position;
            Vector3 localDirectionToGrabber;
            float angle = 0f;
            switch (_adjustmentType)
            {
                case SpatialAdjustmentType.XAxis:
                    localDirectionToGrabber = Vector3.ProjectOnPlane(directionToGrabber, _transformWrapper.up);
                    localDirectionToGrabber.Normalize();
                    angle = Vector3.SignedAngle(-Vector3.forward, localDirectionToGrabber, _transformWrapper.up);
                    if (angle < 0)
                        angle += 360;
                    break;
                case SpatialAdjustmentType.YAxis:
                    localDirectionToGrabber = Vector3.ProjectOnPlane(directionToGrabber, _transformWrapper.right);
                    localDirectionToGrabber.Normalize();
                    angle = Vector3.SignedAngle(Vector3.up, localDirectionToGrabber, _transformWrapper.right);
                    if (angle < 0)
                        angle += 360;
                    break;
                case SpatialAdjustmentType.ZAxis:
                    localDirectionToGrabber = Vector3.ProjectOnPlane(directionToGrabber, _transformWrapper.forward);
                    localDirectionToGrabber.Normalize();
                    Quaternion rotation = Quaternion.FromToRotation(-Vector3.right, localDirectionToGrabber);

                    Debug.Log($"Current: {rotation.eulerAngles.y} | Old: {_oldRotationalValue} | Difference: {rotation.eulerAngles.y - _oldRotationalValue}");

                    if(rotation.eulerAngles.y - _oldRotationalValue < -180)
                        numberOfRevolutions++;
                    else if(rotation.eulerAngles.y - _oldRotationalValue > 180)
                        numberOfRevolutions--;
                    
                    _oldRotationalValue = rotation.eulerAngles.y;

                    angle = rotation.eulerAngles.y + (numberOfRevolutions * 360);
                    angle = Mathf.Clamp(angle, _minimumSpatialValue, _maximumSpatialValue);
                    Debug.Log($"Angle: {angle} | Revolutions: {numberOfRevolutions}");
                    break;
            }

            float angularAdjustment = angle;
            _spatialValue = angularAdjustment;
            float OutputValue = ConvertToOutputValue(_spatialValue);
            SetValueOnStateModule(OutputValue);
        }

        private void SetValueOnStateModule(float value)
        {
            if (_adjustmentProperty == SpatialAdjustmentProperty.Discrete)
                SetValueByStep(value);
            else
                _AdjustableStateModule.OutputValue = value;
        }

        private void UpdateSteps(int steps)
        {
            _numberOfValues = steps;
            SetValueOnStateModule(_AdjustableStateModule.OutputValue);
        }

        private void SetValueByStep(float value)
        {
            value = Mathf.Clamp(value, _AdjustableStateModule.MinimumOutputValue, _AdjustableStateModule.MaximumOutputValue);

            float stepSize = (_AdjustableStateModule.MaximumOutputValue - _AdjustableStateModule.MinimumOutputValue) / _numberOfValues;
            int stepIndex = Mathf.RoundToInt((value - _AdjustableStateModule.MinimumOutputValue) / stepSize);

            float newValue = _AdjustableStateModule.MinimumOutputValue + stepIndex * stepSize;

            _AdjustableStateModule.OutputValue = newValue;
        }

        private float ConvertToSpatialValue(float outputValue)
        {
            return Mathf.Lerp(_minimumSpatialValue, _maximumSpatialValue, Mathf.InverseLerp(_AdjustableStateModule.MinimumOutputValue, _AdjustableStateModule.MaximumOutputValue, outputValue));
        }

        private float ConvertToOutputValue(float spatialValue)
        {
            return Mathf.Lerp(_AdjustableStateModule.MinimumOutputValue, _AdjustableStateModule.MaximumOutputValue, Mathf.InverseLerp(_minimumSpatialValue, _maximumSpatialValue, spatialValue));
        }

        public void TearDown()
        {
            _AdjustableStateModule.TearDown();
            _FreeGrabbableStateModule.TearDown();

            _FreeGrabbableStateModule.OnGrabConfirmed -= OnGrabConfirmed;
            _FreeGrabbableStateModule.OnDropConfirmed -= OnDropConfirmed;
        }

    }
}
