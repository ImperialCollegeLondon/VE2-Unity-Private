using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using VE2.Common;
using VE2.Common.TransformWrapper;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.NonInteractableInterfaces;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    public enum LinearAdjustmentType
    {
        XAxis,
        YAxis,
        ZAxis
    }

    public enum SpatialAdjustmentProperty
    {
        Discrete,
        Continuous
    }

    [Serializable]
    public class LinearAdjustableConfig
    {
        [SerializeField, IgnoreParent] public SpatialAdjustableServiceConfig LinearAdjustableServiceConfig = new();
        [SerializeField, IgnoreParent] public AdjustableStateConfig AdjustableStateConfig = new();
        [SerializeField, IgnoreParent] public FreeGrabbableStateConfig GrabbableStateConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public RangedInteractionConfig RangedInteractionConfig = new();
        [SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
    }

    [Serializable]
    public class SpatialAdjustableServiceConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Spatial Adjustable Settings", ApplyCondition = true)]
        [SerializeField] public LinearAdjustmentType AdjustmentType = LinearAdjustmentType.XAxis;
        [SerializeField] public SpatialAdjustmentProperty AdjustmentProperty = SpatialAdjustmentProperty.Continuous;
        [SerializeField, ShowIf("AdjustmentProperty", SpatialAdjustmentProperty.Discrete)] public int NumberOfValues = 1;
        [SerializeField] public float MinimumSpatialValue = 0f;
        [EndGroup, SerializeField] public float MaximumSpatialValue = 1f;
    }

    public class LinearAdjustableService
    {
        private float _spatialValue;
        public float SpatialValue { get => _spatialValue; set => SetSpatialValue(value); }
        private float _minimumSpatialValue, _maximumSpatialValue;
        public float MinimumSpatialValue { get => _minimumSpatialValue; set => UpdateMinimumSpatialValue(value); }
        public float MaximumSpatialValue { get => _maximumSpatialValue; set => UpdateMaximumSpatialValue(value); }
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
        private readonly LinearAdjustmentType _adjustmentType;

        public LinearAdjustableService(ITransformWrapper transformWrapper, List<IHandheldInteractionModule> handheldInteractions, LinearAdjustableConfig config, VE2Serializable adjustableState, VE2Serializable grabbableState, string id,
            WorldStateModulesContainer worldStateModulesContainer, InteractorContainer interactorContainer)
        {
            _RangedAdjustableInteractionModule = new(transformWrapper, handheldInteractions, config.RangedInteractionConfig, config.GeneralInteractionConfig);

            _transformWrapper = transformWrapper;

            _adjustmentType = config.LinearAdjustableServiceConfig.AdjustmentType;
            _adjustmentProperty = config.LinearAdjustableServiceConfig.AdjustmentProperty;

            if (_adjustmentProperty == SpatialAdjustmentProperty.Discrete)
                _numberOfValues = config.LinearAdjustableServiceConfig.NumberOfValues;

            _minimumSpatialValue = config.LinearAdjustableServiceConfig.MinimumSpatialValue;
            _maximumSpatialValue = config.LinearAdjustableServiceConfig.MaximumSpatialValue;

            _AdjustableStateModule = new(adjustableState, config.AdjustableStateConfig, $"ADJ-{id}", worldStateModulesContainer);
            _FreeGrabbableStateModule = new(grabbableState, config.GrabbableStateConfig, $"FG-{id}", worldStateModulesContainer, interactorContainer, RangedAdjustableInteractionModule);

            _RangedAdjustableInteractionModule.OnLocalInteractorRequestGrab += (InteractorID interactorID) => _FreeGrabbableStateModule.SetGrabbed(interactorID);
            _RangedAdjustableInteractionModule.OnLocalInteractorRequestDrop += (InteractorID interactorID) => _FreeGrabbableStateModule.SetDropped(interactorID);

            _FreeGrabbableStateModule.OnGrabConfirmed += OnGrabConfirmed;
            _FreeGrabbableStateModule.OnDropConfirmed += OnDropConfirmed;

            _AdjustableStateModule.OnValueChangedInternal += (float value) => OnStateValueChanged(value);
                      
            SetValueOnStateModule(config.AdjustableStateConfig.StartingOutputValue);
        }

        private void OnGrabConfirmed()
        {

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
            UnityEngine.Debug.Log($"Spatial Value = {_spatialValue}");

            switch (_adjustmentType)
            {
                case LinearAdjustmentType.XAxis:
                    _transformWrapper.localPosition = new Vector3(_spatialValue, _transformWrapper.localPosition.y, _transformWrapper.localPosition.z);
                    break;
                case LinearAdjustmentType.YAxis:
                    _transformWrapper.localPosition = new Vector3(_transformWrapper.localPosition.x, _spatialValue, _transformWrapper.localPosition.z);
                    break;
                case LinearAdjustmentType.ZAxis:
                    _transformWrapper.localPosition = new Vector3(_transformWrapper.localPosition.x, _transformWrapper.localPosition.y, _spatialValue);
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
            Vector3 localGrabPosition = _transformWrapper.InverseTransfromPoint(grabberPosition);
            float adjustment = 0f;
            switch (_adjustmentType)
            {
                case LinearAdjustmentType.XAxis:
                    adjustment = Mathf.Clamp(localGrabPosition.x, _minimumSpatialValue, _maximumSpatialValue);
                    break;
                case LinearAdjustmentType.YAxis:
                    adjustment = Mathf.Clamp(localGrabPosition.y, _minimumSpatialValue, _maximumSpatialValue);
                    break;
                case LinearAdjustmentType.ZAxis:
                    adjustment = Mathf.Clamp(localGrabPosition.z, _minimumSpatialValue, _maximumSpatialValue);
                    break;
            }

            _spatialValue = adjustment;
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

        private void UpdateMinimumSpatialValue(float value)
        {
            _minimumSpatialValue = value;
            SetValueOnStateModule(_AdjustableStateModule.OutputValue);
        }

        private void UpdateMaximumSpatialValue(float value)
        {
            _maximumSpatialValue = value;
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