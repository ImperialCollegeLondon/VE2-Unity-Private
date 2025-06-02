using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Shared;
using static VE2.Common.Shared.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    internal enum SpatialAdjustmentType
    {
        XAxis,
        YAxis,
        ZAxis
    }

    internal enum SpatialAdjustmentProperty
    {
        Discrete,
        Continuous
    }

    [Serializable]
    internal class LinearAdjustableConfig
    {
        [SerializeField, IgnoreParent] public AdjustableStateConfig AdjustableStateConfig = new();
        [SerializeField, IgnoreParent] public SpatialAdjustableServiceConfig LinearAdjustableServiceConfig = new();
        [SerializeField, IgnoreParent] public GrabbableStateConfig GrabbableStateConfig = new();

        [SpaceArea(spaceAfter: 10), SerializeField, IndentArea(-1)] public RangedAdjustableInteractionConfig rangedAdjustableConfig = new();
        [SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
        
        [HideIf(nameof(MultiplayerSupportPresent), false)]
        [SerializeField, IgnoreParent] public WorldStateSyncConfig SyncConfig = new();

        private bool MultiplayerSupportPresent => VE2API.HasMultiPlayerSupport;
    }

    [Serializable]
    internal class SpatialAdjustableServiceConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Spatial Adjustable Settings", ApplyCondition = true)]
        [SerializeField] public SpatialAdjustmentType AdjustmentType = SpatialAdjustmentType.XAxis;
        [SerializeField] public SpatialAdjustmentProperty AdjustmentProperty = SpatialAdjustmentProperty.Continuous;
        [SerializeField, ShowIf("AdjustmentProperty", SpatialAdjustmentProperty.Discrete)] public int NumberOfValues = 1;
        [SerializeField] public float MinimumSpatialValue = 0f;
        [SerializeField, EndGroup] public float MaximumSpatialValue = 1f;

        // [SerializeField] public bool SinglePressScroll = false;
        // [ShowIf("SinglePressScroll", false)]
        // [EndGroup, SerializeField] public float IncrementPerSecondVRStickHeld = 4;
    }

    internal class LinearAdjustableService
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
        public IGrabbableStateModule FreeGrabbableStateModule => _GrabbableStateModule;
        public IRangedAdjustableInteractionModule RangedAdjustableInteractionModule => _RangedAdjustableInteractionModule;
        #endregion

        #region Modules
        private readonly AdjustableStateModule _AdjustableStateModule;
        private readonly GrabbableStateModule _GrabbableStateModule;
        private readonly RangedAdjustableInteractionModule _RangedAdjustableInteractionModule;
        #endregion

        private readonly SpatialAdjustmentProperty _adjustmentProperty;
        private readonly ITransformWrapper _transformToTranslate;
        private readonly ITransformWrapper _attachPointTransform;
        private readonly SpatialAdjustmentType _adjustmentType;
        private readonly float _incrementPerScrollTick;

        public LinearAdjustableService(ITransformWrapper transformWrapper, List<IHandheldInteractionModule> handheldInteractions, LinearAdjustableConfig config, VE2Serializable adjustableState, VE2Serializable grabbableState, string id,
            IWorldStateSyncableContainer worldStateSyncableContainer, IGrabInteractablesContainer grabInteractablesContainer, HandInteractorContainer interactorContainer, IClientIDWrapper localClientIdWrapper)
        {
            ITransformWrapper transformToTranslate = config.rangedAdjustableConfig.TransformToAdjust == null ? transformWrapper : new TransformWrapper(config.rangedAdjustableConfig.TransformToAdjust);

            //get attach point transform, if null, use the transform wrapper (the object itself)
            _attachPointTransform = config.rangedAdjustableConfig.AttachPoint == null ? transformToTranslate : new TransformWrapper(config.rangedAdjustableConfig.AttachPoint);

            //initialize module for ranged adjustable interaction (scrolling)
            _RangedAdjustableInteractionModule = new(id, grabInteractablesContainer, _attachPointTransform, handheldInteractions, config.rangedAdjustableConfig, config.GeneralInteractionConfig);

            _incrementPerScrollTick = config.AdjustableStateConfig.IncrementPerScrollTick;
            _transformToTranslate = transformToTranslate;

            _adjustmentType = config.LinearAdjustableServiceConfig.AdjustmentType;
            _adjustmentProperty = config.LinearAdjustableServiceConfig.AdjustmentProperty;

            if (_adjustmentProperty == SpatialAdjustmentProperty.Discrete)
                _numberOfValues = config.LinearAdjustableServiceConfig.NumberOfValues;

            _minimumSpatialValue = config.LinearAdjustableServiceConfig.MinimumSpatialValue;
            _maximumSpatialValue = config.LinearAdjustableServiceConfig.MaximumSpatialValue;

            //seperate modules for adjustable state and free grabbable state. Give the adjustable state module a different ID so it doesn't clash in the syncer with the grabbable state module
            //The Grabbable state module needs the same ID that is passed to the ranged adjustable interaction module, so the interactor can pull the module from the grab interactable container
            _AdjustableStateModule = new(adjustableState, config.AdjustableStateConfig, config.SyncConfig, $"ADJ-{id}", worldStateSyncableContainer, localClientIdWrapper);
            _GrabbableStateModule = new(grabbableState, config.GrabbableStateConfig, config.SyncConfig, $"{id}", worldStateSyncableContainer, interactorContainer, localClientIdWrapper);

            _RangedAdjustableInteractionModule.OnLocalInteractorRequestGrab += (InteractorID interactorID) => _GrabbableStateModule.SetGrabbed(interactorID);
            _RangedAdjustableInteractionModule.OnLocalInteractorRequestDrop += (InteractorID interactorID) => _GrabbableStateModule.SetDropped(interactorID);

            _RangedAdjustableInteractionModule.OnScrollUp += OnScrollUp;
            _RangedAdjustableInteractionModule.OnScrollDown += OnScrollDown;

            _GrabbableStateModule.OnGrabConfirmed += OnGrabConfirmed;
            _GrabbableStateModule.OnDropConfirmed += OnDropConfirmed;

            _AdjustableStateModule.OnValueChangedInternal += (float value) => OnStateValueChanged(value);

            //UnityEngine.Debug.Log(config.AdjustableStateConfig.StartingOutputValue);

            //set the initial value of the adjustable state module
            SetValueOnStateModule(config.AdjustableStateConfig.StartingOutputValue);
        }

        private void OnScrollUp()
        {
            float targetValue = _AdjustableStateModule.OutputValue + _incrementPerScrollTick; //should this change spatial value?
            targetValue = Mathf.Clamp(targetValue, _AdjustableStateModule.MinimumOutputValue, _AdjustableStateModule.MaximumOutputValue);
            SetValueOnStateModule(targetValue);
        }

        private void OnScrollDown()
        {
            float targetValue = _AdjustableStateModule.OutputValue - _incrementPerScrollTick; //should this change spatial value?
            targetValue = Mathf.Clamp(targetValue, _AdjustableStateModule.MinimumOutputValue, _AdjustableStateModule.MaximumOutputValue);
            SetValueOnStateModule(targetValue);
        }

        private void OnGrabConfirmed(ushort id)
        {

        }

        private void OnDropConfirmed(ushort id)
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
                    _transformToTranslate.localPosition = new Vector3(_spatialValue, _transformToTranslate.localPosition.y, _transformToTranslate.localPosition.z);
                    break;
                case SpatialAdjustmentType.YAxis:
                    _transformToTranslate.localPosition = new Vector3(_transformToTranslate.localPosition.x, _spatialValue, _transformToTranslate.localPosition.z);
                    break;
                case SpatialAdjustmentType.ZAxis:
                    _transformToTranslate.localPosition = new Vector3(_transformToTranslate.localPosition.x, _transformToTranslate.localPosition.y, _spatialValue);
                    break;
            }
        }

        public void HandleFixedUpdate()
        {
            _GrabbableStateModule.HandleFixedUpdate();
            if (_GrabbableStateModule.IsLocalGrabbed)
            {
                TrackPosition(_GrabbableStateModule.CurrentGrabbingInteractor.GrabberTransform.position);
            }

            _AdjustableStateModule.HandleFixedUpdate();
        }

        private void TrackPosition(Vector3 grabberPosition)
        {
            //get the vector position of the grabber in the local space of the object
            Vector3 localGrabPosition = _transformToTranslate.InverseTransfromPoint(grabberPosition);
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
            //UnityEngine.Debug.Log($"value = {value}, AdjustableStateModule.OutputValue = {_AdjustableStateModule.OutputValue}");

            if (value == _AdjustableStateModule.OutputValue)
                return;

            if (_adjustmentProperty == SpatialAdjustmentProperty.Discrete)
                SetValueByStep(value);
            else
                _AdjustableStateModule.SetOutputValue(value);
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

            _AdjustableStateModule.SetOutputValue(value);
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
            _RangedAdjustableInteractionModule.TearDown();
            _AdjustableStateModule.TearDown();
            _GrabbableStateModule.TearDown();

            _GrabbableStateModule.OnGrabConfirmed -= OnGrabConfirmed;
            _GrabbableStateModule.OnDropConfirmed -= OnDropConfirmed;
        }
    }
}