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
        [SerializeField] public float MinimumOutputValue = 0f;
        [EndGroup, SerializeField] public float MaximumOutputValue = 1f;
    }

    public class LinearAdjustableService
    {
        private float _outputValue;
        public float OutputValue { get => _outputValue; set => SetOutputValue(value); }
        public float MinimumOutputValue, MaximumOutputValue;

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
        private readonly int _numberOfValues;
        private readonly ITransformWrapper _transformWrapper;
        private readonly LinearAdjustmentType _adjustmentType;

        public LinearAdjustableService(ITransformWrapper transformWrapper, List<IHandheldInteractionModule> handheldInteractions, LinearAdjustableConfig config, VE2Serializable adjustableState, VE2Serializable grabbableState, string id,
            WorldStateModulesContainer worldStateModulesContainer, InteractorContainer interactorContainer)
        {
            _RangedAdjustableInteractionModule = new(transformWrapper, handheldInteractions, config.RangedInteractionConfig, config.GeneralInteractionConfig);

            _transformWrapper = transformWrapper;

            _adjustmentType = config.LinearAdjustableServiceConfig.AdjustmentType;
            _adjustmentProperty = config.LinearAdjustableServiceConfig.AdjustmentProperty;

            if(_adjustmentProperty == SpatialAdjustmentProperty.Discrete)
                _numberOfValues = config.LinearAdjustableServiceConfig.NumberOfValues;

            MinimumOutputValue = config.LinearAdjustableServiceConfig.MinimumOutputValue;
            MaximumOutputValue = config.LinearAdjustableServiceConfig.MaximumOutputValue;

            _AdjustableStateModule = new(adjustableState, config.AdjustableStateConfig, $"ADJ-{id}", worldStateModulesContainer);
            _FreeGrabbableStateModule = new(grabbableState, config.GrabbableStateConfig, $"FG-{id}", worldStateModulesContainer, interactorContainer, RangedAdjustableInteractionModule);

            _RangedAdjustableInteractionModule.OnLocalInteractorRequestGrab += (InteractorID interactorID) => _FreeGrabbableStateModule.SetGrabbed(interactorID);
            _RangedAdjustableInteractionModule.OnLocalInteractorRequestDrop += (InteractorID interactorID) => _FreeGrabbableStateModule.SetDropped(interactorID);

            _FreeGrabbableStateModule.OnGrabConfirmed += OnGrabConfirmed;
            _FreeGrabbableStateModule.OnDropConfirmed += OnDropConfirmed;

            _AdjustableStateModule.OnValueChangedInternal += (float value) => OnStateValueChanged(value);

            SetValueOnStateModule(config.AdjustableStateConfig.StartingValue);
        }

        private void OnGrabConfirmed()
        {

        }

        private void OnDropConfirmed()
        {

        }

        private void SetOutputValue(float mappedValue)
        {
            mappedValue = Mathf.Clamp(mappedValue, MinimumOutputValue, MaximumOutputValue);
            _outputValue = mappedValue;
            float rawValue = Mathf.Lerp(_AdjustableStateModule.MinimumValue, _AdjustableStateModule.MaximumValue, Mathf.InverseLerp(MinimumOutputValue, MaximumOutputValue, mappedValue));

            SetValueOnStateModule(rawValue);
        }

        private void OnStateValueChanged(float value)
        {
            switch (_adjustmentType)
            {
                case LinearAdjustmentType.XAxis:
                    _transformWrapper.localPosition = Vector3.right * value;
                    break;
                case LinearAdjustmentType.YAxis:
                    _transformWrapper.localPosition = Vector3.up * value;
                    break;
                case LinearAdjustmentType.ZAxis:
                    _transformWrapper.localPosition = Vector3.forward * value;
                    break;
            }


            OutputValue = Mathf.Lerp(MinimumOutputValue, MaximumOutputValue, Mathf.InverseLerp(_AdjustableStateModule.MinimumValue, _AdjustableStateModule.MaximumValue, value));

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
                    adjustment = Mathf.Clamp(localGrabPosition.x, _AdjustableStateModule.MinimumValue, _AdjustableStateModule.MaximumValue);
                    break;
                case LinearAdjustmentType.YAxis:
                    adjustment = Mathf.Clamp(localGrabPosition.y, _AdjustableStateModule.MinimumValue, _AdjustableStateModule.MaximumValue);
                    break;
                case LinearAdjustmentType.ZAxis:
                    adjustment = Mathf.Clamp(localGrabPosition.z, _AdjustableStateModule.MinimumValue, _AdjustableStateModule.MaximumValue);
                    break;
            }

            SetValueOnStateModule(adjustment);
        }

        private void SetValueOnStateModule(float value)
        {
            if(_adjustmentProperty == SpatialAdjustmentProperty.Discrete)
                SetValueByStep(value);
            else
                _AdjustableStateModule.Value = value;
        }

        private void SetValueByStep(float value)
        {
            value = Mathf.Clamp(value, _AdjustableStateModule.MinimumValue, _AdjustableStateModule.MaximumValue);

            float stepSize = (_AdjustableStateModule.MaximumValue - _AdjustableStateModule.MinimumValue) / _numberOfValues;
            int stepIndex = Mathf.RoundToInt((value - _AdjustableStateModule.MinimumValue)/stepSize);

            float newValue = _AdjustableStateModule.MinimumValue + stepIndex * stepSize;
            
            _AdjustableStateModule.Value = newValue;
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