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
    public enum SpatialAdjustmentType
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
        [SerializeField] public SpatialAdjustmentType AdjustmentType = SpatialAdjustmentType.XAxis;
        [SerializeField] public SpatialAdjustmentProperty AdjustmentProperty = SpatialAdjustmentProperty.Continuous;
        [SerializeField, ShowIf("AdjustmentProperty", SpatialAdjustmentProperty.Discrete)] public int NumberOfValues = 1;
        [SerializeField] public float MinimumSpatialValue = 0f;
        [SerializeField] public float MaximumSpatialValue = 1f;

        // [SerializeField] public bool SinglePressScroll = false;
        // [ShowIf("SinglePressScroll", false)]
        // [EndGroup, SerializeField] public float IncrementPerSecondVRStickHeld = 4;
    }

    public class LinearAdjustableService
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
        private readonly ITransformWrapper _attachPointTransform;
        private readonly SpatialAdjustmentType _adjustmentType;
        private readonly float _incrementPerScrollTick;

        public LinearAdjustableService(ITransformWrapper transformWrapper, List<IHandheldInteractionModule> handheldInteractions, LinearAdjustableConfig config, VE2Serializable adjustableState, VE2Serializable grabbableState, string id,
            WorldStateModulesContainer worldStateModulesContainer, InteractorContainer interactorContainer)
        {
            //get attach point transform, if null, use the transform wrapper (the object itself)
            _attachPointTransform = config.GrabbableStateConfig.AttachPoint == null ? transformWrapper : new TransformWrapper(config.GrabbableStateConfig.AttachPoint);

            //initialize module for ranged adjustable interaction (scrolling)
            _RangedAdjustableInteractionModule = new(_attachPointTransform, handheldInteractions, config.RangedInteractionConfig, config.GeneralInteractionConfig);

            _incrementPerScrollTick = config.AdjustableStateConfig.IncrementPerScrollTick;
            _transformWrapper = transformWrapper;

            _adjustmentType = config.LinearAdjustableServiceConfig.AdjustmentType;
            _adjustmentProperty = config.LinearAdjustableServiceConfig.AdjustmentProperty;

            if (_adjustmentProperty == SpatialAdjustmentProperty.Discrete)
                _numberOfValues = config.LinearAdjustableServiceConfig.NumberOfValues;

            _minimumSpatialValue = config.LinearAdjustableServiceConfig.MinimumSpatialValue;
            _maximumSpatialValue = config.LinearAdjustableServiceConfig.MaximumSpatialValue;

            //seperate modules for adjustable state and free grabbable state, they have a unique ID for each for the world state syncer
            _AdjustableStateModule = new(adjustableState, config.AdjustableStateConfig, $"ADJ-{id}", worldStateModulesContainer);
            _FreeGrabbableStateModule = new(grabbableState, config.GrabbableStateConfig, $"FG-{id}", worldStateModulesContainer, interactorContainer, RangedAdjustableInteractionModule);

            _RangedAdjustableInteractionModule.OnLocalInteractorRequestGrab += (InteractorID interactorID) => _FreeGrabbableStateModule.SetGrabbed(interactorID);
            _RangedAdjustableInteractionModule.OnLocalInteractorRequestDrop += (InteractorID interactorID) => _FreeGrabbableStateModule.SetDropped(interactorID);

            _RangedAdjustableInteractionModule.OnScrollUp += OnScrollUp;
            _RangedAdjustableInteractionModule.OnScrollDown += OnScrollDown;

            _FreeGrabbableStateModule.OnGrabConfirmed += OnGrabConfirmed;
            _FreeGrabbableStateModule.OnDropConfirmed += OnDropConfirmed;

            _AdjustableStateModule.OnValueChangedInternal += (float value) => OnStateValueChanged(value);
                      
            //set the initial value of the adjustable state module
            SetValueOnStateModule(config.AdjustableStateConfig.StartingOutputValue);
        }

        private void OnScrollUp()
        {
            float targetValue = _AdjustableStateModule.OutputValue + _incrementPerScrollTick; //should this change spatial value?
            UnityEngine.Debug.Log($"Scrolling Up: {targetValue}");
            SetValueOnStateModule(targetValue);
        }

        private void OnScrollDown()
        {
            float targetValue = _AdjustableStateModule.OutputValue - _incrementPerScrollTick; //should this change spatial value?
            UnityEngine.Debug.Log($"Scrolling Down: {targetValue}");
            SetValueOnStateModule(targetValue);
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
            //Values received from the state are always output values
            //convert the output value to spatial value
            _spatialValue = ConvertToSpatialValue(value);

            switch (_adjustmentType)
            {
                case SpatialAdjustmentType.XAxis:
                    _attachPointTransform.localPosition = new Vector3(_spatialValue, _attachPointTransform.localPosition.y, _attachPointTransform.localPosition.z);
                    break;
                case SpatialAdjustmentType.YAxis:
                    _attachPointTransform.localPosition = new Vector3(_attachPointTransform.localPosition.x, _spatialValue, _attachPointTransform.localPosition.z);
                    break;
                case SpatialAdjustmentType.ZAxis:
                    _attachPointTransform.localPosition = new Vector3(_attachPointTransform.localPosition.x, _attachPointTransform.localPosition.y, _spatialValue);
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
            //get the vector position of the grabber in the local space of the object
            Vector3 localGrabPosition = _transformWrapper.InverseTransfromPoint(grabberPosition);
            float adjustment = 0f;

            //get the grabber value X/Y/Z based on the adjustment axis X/Y/Z
            switch (_adjustmentType)
            {
                case SpatialAdjustmentType.XAxis:
                    adjustment = Mathf.Clamp(localGrabPosition.x, _minimumSpatialValue, _maximumSpatialValue);
                    break;
                case SpatialAdjustmentType.YAxis:
                    adjustment = Mathf.Clamp(localGrabPosition.y, _minimumSpatialValue, _maximumSpatialValue);
                    break;
                case SpatialAdjustmentType.ZAxis:
                    adjustment = Mathf.Clamp(localGrabPosition.z, _minimumSpatialValue, _maximumSpatialValue);
                    break;
            }

            _spatialValue = adjustment;
            float OutputValue = ConvertToOutputValue(_spatialValue);
            SetValueOnStateModule(OutputValue);
        }

        /* MAKE SURE ANY SPATIAL VALUE IS CONVERTED TO OUTPUT VALUE BEFORE SETTING IT TO THE STATE MODULE
        ALWAYS CALL THIS VALUE TO SET THE VALUE TO THE STATE MODULE, it calculates it automatically based on if it is discrete and continuous
        and sets it to tthe state module */
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

            //get the size between each step based on the number of values provided and the index of the step
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