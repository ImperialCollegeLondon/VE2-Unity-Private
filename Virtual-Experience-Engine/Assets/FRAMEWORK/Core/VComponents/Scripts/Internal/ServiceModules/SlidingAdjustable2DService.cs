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
    internal enum SpatialAdjustmentAxis2D
    {
        XAxis,
        YAxis,
        ZAxis
    }

    internal enum SpatialAdjustmentType2D
{
        Discrete,
        Continuous
    }

    [Serializable]
    internal class SlidingAdjustable2DConfig
    {
        public void OpenDocs() => Application.OpenURL("https://www.notion.so/V_LinearAdjustable-20f0e4d8ed4d8125b125ea8d031e8aeb?source=copy_link");
        [EditorButton(nameof(OpenDocs), "Open Docs", PositionType = ButtonPositionType.Above)]
        [SerializeField, IgnoreParent] public Adjustable2DStateConfig Adjustable2DStateConfig = new();
        [SerializeField, IgnoreParent] public SpatialAdjustableServiceConfig LinearAdjustableServiceConfig = new();
        [SerializeField, IgnoreParent] public GrabbableStateConfig GrabbableStateConfig = new();

        [SpaceArea(spaceAfter: 10), SerializeField, IndentArea(-1)] public RangedAdjustableInteractionConfig RangedAdjustableInteractionConfig = new();
        [SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();

        [HideIf(nameof(MultiplayerSupportPresent), false)]
        [SerializeField, IgnoreParent] public WorldStateSyncConfig SyncConfig = new();

        private bool MultiplayerSupportPresent => VE2API.HasMultiPlayerSupport;

        //Constructor used for tests
        public SlidingAdjustable2DConfig(ITransformWrapper attachPointWrapper, ITransformWrapper transformToMove)
        {
            RangedAdjustableInteractionConfig.AttachPointWrapper = attachPointWrapper;
            RangedAdjustableInteractionConfig.TransformToAdjust = transformToMove;
        }
        public SlidingAdjustable2DConfig() { }
    }

    [Serializable]
    internal class SpatialAdjustable2DServiceConfig
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

    internal class SlidingAdjustable2DService
    {
        private ITransformWrapper _transformToAdjust => _config.RangedAdjustableInteractionConfig.TransformToAdjust;
        private Vector2 _spatialValue;
        public Vector2 SpatialValue { get => _spatialValue; set => SetSpatialValue(value); }
        public Vector2 MinimumSpatialValue { get => _config.LinearAdjustableServiceConfig.MinimumSpatialValue; set => _config.LinearAdjustableServiceConfig.MinimumSpatialValue = value; }
        public Vector2 MaximumSpatialValue { get => _config.LinearAdjustableServiceConfig.MaximumSpatialValue; set => _config.LinearAdjustableServiceConfig.MaximumSpatialValue = value; }
        private int _numberOfValues => _config.LinearAdjustableServiceConfig.NumberOfDiscreteValues;
        public int NumberOfValues { get => _numberOfValues; set => UpdateSteps(value); }

        #region Interfaces
        public IAdjustable2DStateModule AdjustableStateModule => _adjustable2DStateModule;
        public IGrabbableStateModule FreeGrabbableStateModule => _grabbableStateModule;
        public IRangedAdjustableInteractionModule RangedAdjustableInteractionModule => _rangedAdjustableInteractionModule;
        #endregion

        #region Modules
        private readonly Adjustable2DStateModule _adjustable2DStateModule;
        private readonly GrabbableStateModule _grabbableStateModule;
        private readonly RangedAdjustableInteractionModule _rangedAdjustableInteractionModule;
        #endregion

        private readonly SlidingAdjustable2DConfig _config;

        public SlidingAdjustable2DService(List<IHandheldInteractionModule> handheldInteractions, SlidingAdjustable2DConfig config, AdjustableState adjustableState, VE2Serializable grabbableState, string id,
            IWorldStateSyncableContainer worldStateSyncableContainer, IGrabInteractablesContainer grabInteractablesContainer, HandInteractorContainer interactorContainer, IClientIDWrapper localClientIdWrapper)
        {
            _config = config;

            _rangedAdjustableInteractionModule = new(id, grabInteractablesContainer, handheldInteractions, config.RangedAdjustableInteractionConfig, config.GeneralInteractionConfig);

            //seperate modules for adjustable state and free grabbable state. Give the adjustable state module a different ID so it doesn't clash in the syncer with the grabbable state module
            //The Grabbable state module needs the same ID that is passed to the ranged adjustable interaction module, so the interactor can pull the module from the grab interactable container
            _adjustable2DStateModule = new(adjustableState, config.Adjustable2DStateConfig, config.SyncConfig, $"ADJ-{id}", worldStateSyncableContainer, localClientIdWrapper);
            _grabbableStateModule = new(grabbableState, config.GrabbableStateConfig, config.SyncConfig, $"{id}", worldStateSyncableContainer, grabInteractablesContainer, interactorContainer, localClientIdWrapper);

            _rangedAdjustableInteractionModule.OnLocalInteractorRequestGrab += (InteractorID interactorID) => _grabbableStateModule.SetGrabbed(interactorID);
            _rangedAdjustableInteractionModule.OnLocalInteractorRequestDrop += (InteractorID interactorID) => _grabbableStateModule.SetDropped(interactorID);

            _rangedAdjustableInteractionModule.OnScrollUp += HandleScrollUp;
            _rangedAdjustableInteractionModule.OnScrollDown += OnScrollDown;

            _grabbableStateModule.OnGrabConfirmed += HandleGrabConfirmed;
            _grabbableStateModule.OnDropConfirmed += HandleDropConfirmed;

            _adjustable2DStateModule.OnValueChangedInternal += (Vector2 value) => HandleStateValueChanged(value);
        }

        public void HandleStart() => _adjustable2DStateModule.InitializeStateWithStartingValue();

        private void HandleScrollUp(ushort clientID)
        {
            float scrollMultiplier = _config.LinearAdjustableServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete ? (_adjustable2DStateModule.MaximumOutputValue - _adjustable2DStateModule.MinimumOutputValue) / (_numberOfValues - 1) : _config.Adjustable2DStateConfig.IncrementPerScrollTick;
            float targetValue = _adjustable2DStateModule.OutputValue + scrollMultiplier; //should this change spatial value?
            targetValue = Mathf.Clamp(targetValue, _adjustable2DStateModule.MinimumOutputValue, _adjustable2DStateModule.MaximumOutputValue);
            SetValueOnStateModule(targetValue, clientID);
        }

        private void OnScrollDown(ushort clientID)
        {
            float scrollMultiplier = _config.LinearAdjustableServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete ? (_adjustable2DStateModule.MaximumOutputValue - _adjustable2DStateModule.MinimumOutputValue) / (_numberOfValues - 1) : _config.Adjustable2DStateConfig.IncrementPerScrollTick;
            float targetValue = _adjustable2DStateModule.OutputValue - scrollMultiplier; //should this change spatial value?
            targetValue = Mathf.Clamp(targetValue, _adjustable2DStateModule.MinimumOutputValue, _adjustable2DStateModule.MaximumOutputValue);
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

        private void HandleStateValueChanged(Vector2 value)
        {
            //Values received from the state are always output values
            //convert the output value to spatial value
            Vector2 newSpatialValue = ConvertToSpatialValue(value);

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

            _adjustable2DStateModule.HandleFixedUpdate();
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

            if (value == _adjustable2DStateModule.OutputValue)
                return;

            if (_config.LinearAdjustableServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete)
                SetValueByStep(value, clientID);
            else
                _adjustable2DStateModule.SetOutputValueInternal(value, clientID);
        }

        private void UpdateSteps(int steps)
        {
            _config.LinearAdjustableServiceConfig.NumberOfDiscreteValues = steps;
            SetValueOnStateModule(_adjustable2DStateModule.OutputValue);
        }

        private void SetValueByStep(float value, ushort clientID)
        {
            value = Mathf.Clamp(value, _adjustable2DStateModule.MinimumOutputValue, _adjustable2DStateModule.MaximumOutputValue);

            //get the size between each step based on the number of values provided and the index of the step
            float stepSize = (_adjustable2DStateModule.MaximumOutputValue - _adjustable2DStateModule.MinimumOutputValue) / (_numberOfValues - 1); // -1 because works the same way as the index of an array
            int stepIndex = Mathf.RoundToInt((value - _adjustable2DStateModule.MinimumOutputValue) / stepSize);

            float newValue = _adjustable2DStateModule.MinimumOutputValue + stepIndex * stepSize;

            _adjustable2DStateModule.SetOutputValueInternal(newValue, clientID);
        }

        private Vector2 ConvertToSpatialValue(Vector2 outputValue)
        {
            return new Vector2(
               Mathf.Lerp(MinimumSpatialValue, MaximumSpatialValue, Mathf.InverseLerp(_adjustable2DStateModule.MinimumOutputValue.x, _adjustable2DStateModule.MaximumOutputValue.x, outputValue.x)),
               Mathf.Lerp(MinimumSpatialValue, MaximumSpatialValue, Mathf.InverseLerp(_adjustable2DStateModule.MinimumOutputValue.y, _adjustable2DStateModule.MaximumOutputValue.y, outputValue.y))
            );
        }

        private Vector2 ConvertToOutputValue(Vector2 spatialValue)
        {
            return new Vector2(
               Mathf.Lerp(_adjustable2DStateModule.MinimumOutputValue.x, _adjustable2DStateModule.MaximumOutputValue.x, Mathf.InverseLerp(MinimumSpatialValue, MaximumSpatialValue, spatialValue.x)),
               Mathf.Lerp(_adjustable2DStateModule.MinimumOutputValue.y, _adjustable2DStateModule.MaximumOutputValue.y, Mathf.InverseLerp(MinimumSpatialValue, MaximumSpatialValue, spatialValue.y))
            );
        }

        public void TearDown()
        {
            _rangedAdjustableInteractionModule.TearDown();
            _adjustable2DStateModule.TearDown();
            _grabbableStateModule.TearDown();

            _grabbableStateModule.OnGrabConfirmed -= HandleGrabConfirmed;
            _grabbableStateModule.OnDropConfirmed -= HandleDropConfirmed;
        }
    }
}