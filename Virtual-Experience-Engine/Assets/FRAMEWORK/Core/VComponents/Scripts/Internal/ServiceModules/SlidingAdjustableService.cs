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
    internal class SlidingAdjustableConfig
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
        public SlidingAdjustableConfig(ITransformWrapper attachPointWrapper, ITransformWrapper transformToMove)
        {
            RangedAdjustableInteractionConfig.AttachPointWrapper = attachPointWrapper;
            RangedAdjustableInteractionConfig.TransformToAdjust = transformToMove;
        }
        public SlidingAdjustableConfig() { }
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

    internal class SlidingAdjustableService
    {
        private ITransformWrapper _transformToAdjust => _config.RangedAdjustableInteractionConfig.TransformToAdjust;
        private float _spatialValue;
        public float SpatialValue { get => _spatialValue; set => SetSpatialValue(value); }
        public float MinimumSpatialValue { get => _config.LinearAdjustableServiceConfig.MinimumSpatialValue; set => _config.LinearAdjustableServiceConfig.MinimumSpatialValue = value; }
        public float MaximumSpatialValue { get => _config.LinearAdjustableServiceConfig.MaximumSpatialValue; set => _config.LinearAdjustableServiceConfig.MaximumSpatialValue = value; }
        private int _numberOfValues => _config.LinearAdjustableServiceConfig.NumberOfDiscreteValues;
        public int NumberOfValues { get => _numberOfValues; set => UpdateSteps(value); }

        #region Interfaces
        public IAdjustableStateModule AdjustableStateModule => _adjustableStateModule;
        public IGrabbableStateModule FreeGrabbableStateModule => _grabbableStateModule;
        public IRangedAdjustableInteractionModule RangedAdjustableInteractionModule => _rangedAdjustableInteractionModule;
        #endregion

        #region Modules
        private readonly AdjustableStateModule _adjustableStateModule;
        private readonly GrabbableStateModule _grabbableStateModule;
        private readonly RangedAdjustableInteractionModule _rangedAdjustableInteractionModule;
        #endregion

        private readonly SlidingAdjustableConfig _config;

        public SlidingAdjustableService(List<IHandheldInteractionModule> handheldInteractions, SlidingAdjustableConfig config, AdjustableState adjustableState, VE2Serializable grabbableState, string id,
            IWorldStateSyncableContainer worldStateSyncableContainer, IGrabInteractablesContainer grabInteractablesContainer, HandInteractorContainer interactorContainer, IClientIDWrapper localClientIdWrapper)
        {
            _config = config;

            _rangedAdjustableInteractionModule = new(id, grabInteractablesContainer, handheldInteractions, config.RangedAdjustableInteractionConfig, config.GeneralInteractionConfig);

            //seperate modules for adjustable state and free grabbable state. Give the adjustable state module a different ID so it doesn't clash in the syncer with the grabbable state module
            //The Grabbable state module needs the same ID that is passed to the ranged adjustable interaction module, so the interactor can pull the module from the grab interactable container
            _adjustableStateModule = new(adjustableState, config.AdjustableStateConfig, config.SyncConfig, $"ADJ-{id}", worldStateSyncableContainer, localClientIdWrapper);
            _grabbableStateModule = new(grabbableState, config.GrabbableStateConfig, config.SyncConfig, $"{id}", worldStateSyncableContainer, interactorContainer, localClientIdWrapper);

            _rangedAdjustableInteractionModule.OnLocalInteractorRequestGrab += (InteractorID interactorID) => _grabbableStateModule.SetGrabbed(interactorID);
            _rangedAdjustableInteractionModule.OnLocalInteractorRequestDrop += (InteractorID interactorID) => _grabbableStateModule.SetDropped(interactorID);

            _rangedAdjustableInteractionModule.OnScrollUp += HandleScrollUp;
            _rangedAdjustableInteractionModule.OnScrollDown += OnScrollDown;

            _grabbableStateModule.OnGrabConfirmed += HandleGrabConfirmed;
            _grabbableStateModule.OnDropConfirmed += HandleDropConfirmed;

            _adjustableStateModule.OnValueChangedInternal += (float value) => HandleStateValueChanged(value);
        }

        public void HandleStart() => _adjustableStateModule.InitializeStateWithStartingValue();

        private void HandleScrollUp(ushort clientID)
        {
            float scrollMultiplier = _config.LinearAdjustableServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete ? (_adjustableStateModule.MaximumOutputValue - _adjustableStateModule.MinimumOutputValue) / (_numberOfValues - 1) : _config.AdjustableStateConfig.IncrementPerScrollTick;
            float targetValue = _adjustableStateModule.OutputValue + scrollMultiplier; //should this change spatial value?
            targetValue = Mathf.Clamp(targetValue, _adjustableStateModule.MinimumOutputValue, _adjustableStateModule.MaximumOutputValue);
            SetValueOnStateModule(targetValue, clientID);
        }

        private void OnScrollDown(ushort clientID)
        {
            float scrollMultiplier = _config.LinearAdjustableServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete ? (_adjustableStateModule.MaximumOutputValue - _adjustableStateModule.MinimumOutputValue) / (_numberOfValues - 1) : _config.AdjustableStateConfig.IncrementPerScrollTick;
            float targetValue = _adjustableStateModule.OutputValue - scrollMultiplier; //should this change spatial value?
            targetValue = Mathf.Clamp(targetValue, _adjustableStateModule.MinimumOutputValue, _adjustableStateModule.MaximumOutputValue);
            SetValueOnStateModule(targetValue, clientID);
        }

        private void HandleGrabConfirmed(ushort id) { }

        private void HandleDropConfirmed(ushort id) { }

        private void SetSpatialValue(float spatialValue)
        {
            _spatialValue = Mathf.Clamp(spatialValue, MinimumSpatialValue, MaximumSpatialValue);
            float OutputValue = ConvertToOutputValue(_spatialValue);
            SetValueOnStateModule(OutputValue);
        }

        private void HandleStateValueChanged(float value)
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

            _rangedAdjustableInteractionModule.NotifyValueChanged();
        }

        public void HandleFixedUpdate()
        {
            _grabbableStateModule.HandleFixedUpdate();
            if (_grabbableStateModule.IsLocalGrabbed)
            {
                TrackPosition(_grabbableStateModule.CurrentGrabbingInteractor.GrabberTransformWrapper.position);
            }

            _adjustableStateModule.HandleFixedUpdate();
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
            SetValueOnStateModule(OutputValue, _grabbableStateModule.MostRecentInteractingClientID.Value);
        }

        /* MAKE SURE ANY SPATIAL VALUE IS CONVERTED TO OUTPUT VALUE BEFORE SETTING IT TO THE STATE MODULE
        ALWAYS CALL THIS VALUE TO SET THE VALUE TO THE STATE MODULE, it calculates it automatically based on if it is discrete and continuous
        and sets it to tthe state module */
        private void SetValueOnStateModule(float value, ushort clientID = ushort.MaxValue)
        {
            //UnityEngine.Debug.Log($"value = {value}, AdjustableStateModule.OutputValue = {_AdjustableStateModule.OutputValue}");

            if (value == _adjustableStateModule.OutputValue)
                return;

            if (_config.LinearAdjustableServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete)
                SetValueByStep(value, clientID);
            else
                _adjustableStateModule.SetOutputValueInternal(value, clientID);
        }

        private void UpdateSteps(int steps)
        {
            _config.LinearAdjustableServiceConfig.NumberOfDiscreteValues = steps;
            SetValueOnStateModule(_adjustableStateModule.OutputValue);
        }

        private void SetValueByStep(float value, ushort clientID)
        {
            value = Mathf.Clamp(value, _adjustableStateModule.MinimumOutputValue, _adjustableStateModule.MaximumOutputValue);

            //get the size between each step based on the number of values provided and the index of the step
            float stepSize = (_adjustableStateModule.MaximumOutputValue - _adjustableStateModule.MinimumOutputValue) / (_numberOfValues - 1); // -1 because works the same way as the index of an array
            int stepIndex = Mathf.RoundToInt((value - _adjustableStateModule.MinimumOutputValue) / stepSize);

            float newValue = _adjustableStateModule.MinimumOutputValue + stepIndex * stepSize;

            _adjustableStateModule.SetOutputValueInternal(newValue, clientID);
        }

        private float ConvertToSpatialValue(float outputValue)
        {
            return Mathf.Lerp(MinimumSpatialValue, MaximumSpatialValue, Mathf.InverseLerp(_adjustableStateModule.MinimumOutputValue, _adjustableStateModule.MaximumOutputValue, outputValue));
        }

        private float ConvertToOutputValue(float spatialValue)
        {
            return Mathf.Lerp(_adjustableStateModule.MinimumOutputValue, _adjustableStateModule.MaximumOutputValue, Mathf.InverseLerp(MinimumSpatialValue, MaximumSpatialValue, spatialValue));
        }

        public void TearDown()
        {
            _rangedAdjustableInteractionModule.TearDown();
            _adjustableStateModule.TearDown();
            _grabbableStateModule.TearDown();

            _grabbableStateModule.OnGrabConfirmed -= HandleGrabConfirmed;
            _grabbableStateModule.OnDropConfirmed -= HandleDropConfirmed;
        }
    }
}