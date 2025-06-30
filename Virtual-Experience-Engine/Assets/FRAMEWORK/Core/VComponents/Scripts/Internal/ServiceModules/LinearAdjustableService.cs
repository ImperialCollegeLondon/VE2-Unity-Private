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
    internal enum SpatialAdjustmentAxis
    {
        XAxis,
        YAxis,
        ZAxis
    }

    internal enum SpatialAdjustmentType
    {
        Discrete,
        Continuous
    }

    [Serializable]
    internal class LinearAdjustableConfig
    {
        public void OpenDocs() => Application.OpenURL("https://www.notion.so/V_LinearAdjustable-20f0e4d8ed4d8125b125ea8d031e8aeb?source=copy_link");
        [EditorButton(nameof(OpenDocs), "Open Docs", PositionType = ButtonPositionType.Above)]
        [SerializeField, IgnoreParent] public AdjustableStateConfig AdjustableStateConfig = new();
        [SerializeField, IgnoreParent] public SpatialAdjustableServiceConfig LinearAdjustableServiceConfig = new();
        [SerializeField, IgnoreParent] public GrabbableStateConfig GrabbableStateConfig = new();

        [SpaceArea(spaceAfter: 10), SerializeField, IndentArea(-1)] public RangedAdjustableInteractionConfig RangedAdjustableInteractionConfig = new();
        [SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();

        [HideIf(nameof(MultiplayerSupportPresent), false)]
        [SerializeField, IgnoreParent] public WorldStateSyncConfig SyncConfig = new();

        private bool MultiplayerSupportPresent => VE2API.HasMultiPlayerSupport;

        //Constructor used for tests
        public LinearAdjustableConfig(ITransformWrapper attachPointWrapper, ITransformWrapper transformToMove)
        {
            RangedAdjustableInteractionConfig.AttachPointWrapper = attachPointWrapper;
            RangedAdjustableInteractionConfig.TransformToAdjust = transformToMove;
        }
        public LinearAdjustableConfig() { }
    }

    [Serializable]
    internal class SpatialAdjustableServiceConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Spatial Adjustable Settings", ApplyCondition = true)]
        [SerializeField] public SpatialAdjustmentAxis AdjustmentAxis = SpatialAdjustmentAxis.XAxis;
        [SerializeField] public SpatialAdjustmentType AdjustmentType = SpatialAdjustmentType.Continuous;
        [SerializeField, ShowIf(nameof(AdjustmentType), SpatialAdjustmentType.Discrete)] public int NumberOfDiscreteValues = 1;
        [SerializeField] public float MinimumSpatialValue = 0f;
        [SerializeField, EndGroup] public float MaximumSpatialValue = 1f;

        // [SerializeField] public bool SinglePressScroll = false;
        // [ShowIf("SinglePressScroll", false)]
        // [EndGroup, SerializeField] public float IncrementPerSecondVRStickHeld = 4;
    }

    internal class LinearAdjustableService
    {
        private ITransformWrapper _transformToAdjust => _config.RangedAdjustableInteractionConfig.TransformToAdjust;
        private float _spatialValue;
        public float SpatialValue { get => _spatialValue; set => SetSpatialValue(value); }
        public float MinimumSpatialValue { get => _config.LinearAdjustableServiceConfig.MinimumSpatialValue; set => _config.LinearAdjustableServiceConfig.MinimumSpatialValue = value; }
        public float MaximumSpatialValue { get => _config.LinearAdjustableServiceConfig.MaximumSpatialValue; set => _config.LinearAdjustableServiceConfig.MaximumSpatialValue = value; }
        private int _numberOfValues => _config.LinearAdjustableServiceConfig.NumberOfDiscreteValues;
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

        private readonly LinearAdjustableConfig _config;

        public LinearAdjustableService(List<IHandheldInteractionModule> handheldInteractions, LinearAdjustableConfig config, AdjustableState adjustableState, VE2Serializable grabbableState, string id,
            IWorldStateSyncableContainer worldStateSyncableContainer, IGrabInteractablesContainer grabInteractablesContainer, HandInteractorContainer interactorContainer, IClientIDWrapper localClientIdWrapper)
        {
            _config = config;

            _RangedAdjustableInteractionModule = new(id, grabInteractablesContainer, handheldInteractions, config.RangedAdjustableInteractionConfig, config.GeneralInteractionConfig);

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
            if (!adjustableState.IsInitialised)
                SetValueOnStateModule(config.AdjustableStateConfig.StartingOutputValue);
            adjustableState.IsInitialised = true;
        }

        private void OnScrollUp()
        {
            float scrollMultiplier = _config.LinearAdjustableServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete ? (_AdjustableStateModule.MaximumOutputValue - _AdjustableStateModule.MinimumOutputValue) / (_numberOfValues - 1) : _config.AdjustableStateConfig.IncrementPerScrollTick;
            float targetValue = _AdjustableStateModule.OutputValue + scrollMultiplier; //should this change spatial value?
            targetValue = Mathf.Clamp(targetValue, _AdjustableStateModule.MinimumOutputValue, _AdjustableStateModule.MaximumOutputValue);
            SetValueOnStateModule(targetValue);
        }

        private void OnScrollDown()
        {
            float scrollMultiplier = _config.LinearAdjustableServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete ? (_AdjustableStateModule.MaximumOutputValue - _AdjustableStateModule.MinimumOutputValue) / (_numberOfValues - 1) : _config.AdjustableStateConfig.IncrementPerScrollTick;
            float targetValue = _AdjustableStateModule.OutputValue - scrollMultiplier; //should this change spatial value?
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
            _spatialValue = Mathf.Clamp(spatialValue, MinimumSpatialValue, MaximumSpatialValue);
            float OutputValue = ConvertToOutputValue(_spatialValue);
            SetValueOnStateModule(OutputValue);
        }

        private void OnStateValueChanged(float value)
        {
            //Values received from the state are always output values
            //convert the output value to spatial value
            float newSpatialValue = ConvertToSpatialValue(value);

            // if (newSpatialValue == _spatialValue)
            //     return;

            _spatialValue = newSpatialValue;

            switch (_config.LinearAdjustableServiceConfig.AdjustmentAxis)
            {
                case SpatialAdjustmentAxis.XAxis:
                    _transformToAdjust.localPosition = new Vector3(_spatialValue, _transformToAdjust.localPosition.y, _transformToAdjust.localPosition.z);
                    break;
                case SpatialAdjustmentAxis.YAxis:
                    _transformToAdjust.localPosition = new Vector3(_transformToAdjust.localPosition.x, _spatialValue, _transformToAdjust.localPosition.z);
                    break;
                case SpatialAdjustmentAxis.ZAxis:
                    _transformToAdjust.localPosition = new Vector3(_transformToAdjust.localPosition.x, _transformToAdjust.localPosition.y, _spatialValue);
                    break;
            }

            _RangedAdjustableInteractionModule.NotifyValueChanged();
        }

        public void HandleFixedUpdate()
        {
            _GrabbableStateModule.HandleFixedUpdate();
            if (_GrabbableStateModule.IsLocalGrabbed)
            {
                TrackPosition(_GrabbableStateModule.CurrentGrabbingInteractor.GrabberTransformWrapper.position);
            }

            _AdjustableStateModule.HandleFixedUpdate();
        }

        private void TrackPosition(Vector3 grabberPosition)
        {
            //get the vector position of the grabber in the local space of the object
            Vector3 localGrabPosition = _transformToAdjust.InverseTransfromPoint(grabberPosition);
            float adjustment = 0f;

            //get the grabber value X/Y/Z based on the adjustment axis X/Y/Z
            switch (_config.LinearAdjustableServiceConfig.AdjustmentAxis)
            {
                case SpatialAdjustmentAxis.XAxis:
                    adjustment = Mathf.Clamp(localGrabPosition.x, MinimumSpatialValue, MaximumSpatialValue);
                    break;
                case SpatialAdjustmentAxis.YAxis:
                    adjustment = Mathf.Clamp(localGrabPosition.y, MinimumSpatialValue, MaximumSpatialValue);
                    break;
                case SpatialAdjustmentAxis.ZAxis:
                    adjustment = Mathf.Clamp(localGrabPosition.z, MinimumSpatialValue, MaximumSpatialValue);
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

            if (_config.LinearAdjustableServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete)
                SetValueByStep(value);
            else
                _AdjustableStateModule.SetOutputValue(value);
        }

        private void UpdateSteps(int steps)
        {
            _config.LinearAdjustableServiceConfig.NumberOfDiscreteValues = steps;
            SetValueOnStateModule(_AdjustableStateModule.OutputValue);
        }

        private void SetValueByStep(float value)
        {
            value = Mathf.Clamp(value, _AdjustableStateModule.MinimumOutputValue, _AdjustableStateModule.MaximumOutputValue);

            //get the size between each step based on the number of values provided and the index of the step
            float stepSize = (_AdjustableStateModule.MaximumOutputValue - _AdjustableStateModule.MinimumOutputValue) / (_numberOfValues - 1); // -1 because works the same way as the index of an array
            int stepIndex = Mathf.RoundToInt((value - _AdjustableStateModule.MinimumOutputValue) / stepSize);

            float newValue = _AdjustableStateModule.MinimumOutputValue + stepIndex * stepSize;

            _AdjustableStateModule.SetOutputValue(newValue);
        }

        private float ConvertToSpatialValue(float outputValue)
        {
            return Mathf.Lerp(MinimumSpatialValue, MaximumSpatialValue, Mathf.InverseLerp(_AdjustableStateModule.MinimumOutputValue, _AdjustableStateModule.MaximumOutputValue, outputValue));
        }

        private float ConvertToOutputValue(float spatialValue)
        {
            return Mathf.Lerp(_AdjustableStateModule.MinimumOutputValue, _AdjustableStateModule.MaximumOutputValue, Mathf.InverseLerp(MinimumSpatialValue, MaximumSpatialValue, spatialValue));
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