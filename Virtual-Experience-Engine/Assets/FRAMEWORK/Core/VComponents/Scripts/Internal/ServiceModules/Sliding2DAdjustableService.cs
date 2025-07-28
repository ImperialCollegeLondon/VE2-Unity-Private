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
        XZAxis,
        XYAxis
    }

    internal enum SpatialAdjustmentType2D
{
        Discrete,
        Continuous
    }

    [Serializable]
    internal class Sliding2DAdjustableConfig
    {
        public void OpenDocs() => Application.OpenURL("https://www.notion.so/V_LinearAdjustable-20f0e4d8ed4d8125b125ea8d031e8aeb?source=copy_link");
        [EditorButton(nameof(OpenDocs), "Open Docs", PositionType = ButtonPositionType.Above)]
        [SerializeField, IgnoreParent] public Adjustable2DStateConfig Adjustable2DStateConfig = new();
        [SerializeField, IgnoreParent] public SpatialAdjustable2DServiceConfig LinearAdjustableServiceConfig = new();
        [SerializeField, IgnoreParent] public GrabbableStateConfig GrabbableStateConfig = new();

        [SpaceArea(spaceAfter: 10), SerializeField, IndentArea(-1)] public RangedAdjustableInteractionConfig RangedAdjustableInteractionConfig = new();
        [SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();

        [HideIf(nameof(MultiplayerSupportPresent), false)]
        [SerializeField, IgnoreParent] public WorldStateSyncConfig SyncConfig = new();

        private bool MultiplayerSupportPresent => VE2API.HasMultiPlayerSupport;

        //Constructor used for tests
        public Sliding2DAdjustableConfig(ITransformWrapper attachPointWrapper, ITransformWrapper transformToMove)
        {
            RangedAdjustableInteractionConfig.AttachPointWrapper = attachPointWrapper;
            RangedAdjustableInteractionConfig.TransformToAdjust = transformToMove;
        }
        public Sliding2DAdjustableConfig() { }
    }

    [Serializable]
    internal class SpatialAdjustable2DServiceConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Spatial Adjustable Settings", ApplyCondition = true)]
        [SerializeField] public SpatialAdjustmentAxis2D AdjustmentAxis = SpatialAdjustmentAxis2D.XZAxis;
        [SerializeField] public SpatialAdjustmentType AdjustmentType = SpatialAdjustmentType.Continuous;
        [SerializeField, ShowIf(nameof(AdjustmentType), SpatialAdjustmentType.Discrete)] public int NumberOfDiscreteValues = 1;
        [SerializeField] public Vector2 MinimumSpatialValue = new Vector2(0,0);
        [SerializeField, EndGroup] public Vector2 MaximumSpatialValue = new Vector2(1, 1);

        // [SerializeField] public bool SinglePressScroll = false;
        // [ShowIf("SinglePressScroll", false)]
        // [EndGroup, SerializeField] public float IncrementPerSecondVRStickHeld = 4;
    }

    internal class Sliding2DAdjustableService
    {
        private ITransformWrapper _transformToAdjust => _config.RangedAdjustableInteractionConfig.TransformToAdjust;
        private Vector2 _spatialValue;
        public Vector2 SpatialValue { get => _spatialValue; set => SetSpatialValue(value); }
        public Vector2 MinimumSpatialValue
        {
            get => new Vector2(_config.LinearAdjustableServiceConfig.MinimumSpatialValue.x, _config.LinearAdjustableServiceConfig.MinimumSpatialValue.y);
            set
            {
                _config.LinearAdjustableServiceConfig.MinimumSpatialValue = value;
            }
        }
        public Vector2 MaximumSpatialValue
        {
            get => new Vector2(_config.LinearAdjustableServiceConfig.MaximumSpatialValue.x, _config.LinearAdjustableServiceConfig.MaximumSpatialValue.y);
            set
            {
                _config.LinearAdjustableServiceConfig.MaximumSpatialValue = value;
            }
        }
        private int _numberOfValues => _config.LinearAdjustableServiceConfig.NumberOfDiscreteValues;
        public int NumberOfValues { get => _numberOfValues; set => UpdateSteps(value); }

        #region Interfaces
        public IAdjustable2DStateModule Adjustable2DStateModule => _adjustable2DStateModule;
        public IGrabbableStateModule FreeGrabbableStateModule => _grabbableStateModule;
        public IRangedAdjustable2DInteractionModule RangedAdjustable2DInteractionModule => _rangedAdjustable2DInteractionModule;
        #endregion

        #region Modules
        private readonly Adjustable2DStateModule _adjustable2DStateModule;
        private readonly GrabbableStateModule _grabbableStateModule;
        private readonly RangedAdjustable2DInteractionModule _rangedAdjustable2DInteractionModule;
        #endregion

        private readonly Sliding2DAdjustableConfig _config;

        public Sliding2DAdjustableService(List<IHandheldInteractionModule> handheldInteractions, Sliding2DAdjustableConfig config, Adjustable2DState adjustableState, VE2Serializable grabbableState, string id,
            IWorldStateSyncableContainer worldStateSyncableContainer, IGrabInteractablesContainer grabInteractablesContainer, HandInteractorContainer interactorContainer, IClientIDWrapper localClientIdWrapper)
        {
            _config = config;

            _rangedAdjustable2DInteractionModule = new(id, grabInteractablesContainer, handheldInteractions, config.RangedAdjustableInteractionConfig, config.GeneralInteractionConfig);

            //seperate modules for adjustable state and free grabbable state. Give the adjustable state module a different ID so it doesn't clash in the syncer with the grabbable state module
            //The Grabbable state module needs the same ID that is passed to the ranged adjustable interaction module, so the interactor can pull the module from the grab interactable container
            _adjustable2DStateModule = new(adjustableState, config.Adjustable2DStateConfig, config.SyncConfig, $"ADJ-{id}", worldStateSyncableContainer, localClientIdWrapper);
            _grabbableStateModule = new(grabbableState, config.GrabbableStateConfig, config.SyncConfig, $"{id}", worldStateSyncableContainer, grabInteractablesContainer, interactorContainer, localClientIdWrapper);

            _rangedAdjustable2DInteractionModule.OnLocalInteractorRequestGrab += (InteractorID interactorID) => _grabbableStateModule.SetGrabbed(interactorID);
            _rangedAdjustable2DInteractionModule.OnLocalInteractorRequestDrop += (InteractorID interactorID) => _grabbableStateModule.SetDropped(interactorID);

            _rangedAdjustable2DInteractionModule.OnScrollUp += HandleScrollUp;
            _rangedAdjustable2DInteractionModule.OnScrollDown += HandleScrollDown;
            _rangedAdjustable2DInteractionModule.OnScrollLeft += HandleScrollLeft;
            _rangedAdjustable2DInteractionModule.OnScrollRight += HandleScrollRight;

            _grabbableStateModule.OnGrabConfirmed += HandleGrabConfirmed;
            _grabbableStateModule.OnDropConfirmed += HandleDropConfirmed;

            _adjustable2DStateModule.OnValueChangedInternal += (Vector2 value) => HandleStateValueChanged(value);
        }

        public void HandleStart() => _adjustable2DStateModule.InitializeStateWithStartingValue();

        private void HandleScrollUp(ushort clientID)
        {
            float scrollMultiplier = _config.LinearAdjustableServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete
                ? (_adjustable2DStateModule.MaximumOutputValue.y - _adjustable2DStateModule.MinimumOutputValue.y) / (_numberOfValues - 1)
                : _config.Adjustable2DStateConfig.IncrementPerScrollTick;

            float targetValueY = _adjustable2DStateModule.OutputValue.y + scrollMultiplier;
            targetValueY = Mathf.Clamp(targetValueY, _adjustable2DStateModule.MinimumOutputValue.y, _adjustable2DStateModule.MaximumOutputValue.y);

            Vector2 targetValue = new Vector2(_adjustable2DStateModule.OutputValue.x, targetValueY);
            SetValueOnStateModule(targetValue, clientID);
        }

        private void HandleScrollDown(ushort clientID)
        {
            float scrollMultiplier = _config.LinearAdjustableServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete
                ? (_adjustable2DStateModule.MaximumOutputValue.y - _adjustable2DStateModule.MinimumOutputValue.y) / (_numberOfValues - 1)
                : _config.Adjustable2DStateConfig.IncrementPerScrollTick;

            float targetValueY = _adjustable2DStateModule.OutputValue.y - scrollMultiplier;
            targetValueY = Mathf.Clamp(targetValueY, _adjustable2DStateModule.MinimumOutputValue.y, _adjustable2DStateModule.MaximumOutputValue.y);

            Vector2 targetValue = new Vector2(_adjustable2DStateModule.OutputValue.x, targetValueY);
            SetValueOnStateModule(targetValue, clientID);
        }

        private void HandleScrollLeft(ushort clientID)
        {
            float scrollMultiplier = _config.LinearAdjustableServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete
                ? (_adjustable2DStateModule.MaximumOutputValue.x - _adjustable2DStateModule.MinimumOutputValue.x) / (_numberOfValues - 1)
                : _config.Adjustable2DStateConfig.IncrementPerScrollTick;

            float targetValueX = _adjustable2DStateModule.OutputValue.x - scrollMultiplier;
            targetValueX = Mathf.Clamp(targetValueX, _adjustable2DStateModule.MinimumOutputValue.x, _adjustable2DStateModule.MaximumOutputValue.x);

            Vector2 targetValue = new Vector2(targetValueX, _adjustable2DStateModule.OutputValue.y);
            SetValueOnStateModule(targetValue, clientID);
        }

        private void HandleScrollRight(ushort clientID)
        {
            float scrollMultiplier = _config.LinearAdjustableServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete
                ? (_adjustable2DStateModule.MaximumOutputValue.x - _adjustable2DStateModule.MinimumOutputValue.x) / (_numberOfValues - 1)
                : _config.Adjustable2DStateConfig.IncrementPerScrollTick;

            float targetValueX = _adjustable2DStateModule.OutputValue.x + scrollMultiplier;
            targetValueX = Mathf.Clamp(targetValueX, _adjustable2DStateModule.MinimumOutputValue.x, _adjustable2DStateModule.MaximumOutputValue.x);

            Vector2 targetValue = new Vector2(targetValueX, _adjustable2DStateModule.OutputValue.y);
            SetValueOnStateModule(targetValue, clientID);
        }

        private void HandleGrabConfirmed(ushort id) { }

        private void HandleDropConfirmed(ushort id) { }

        private void SetSpatialValue(Vector2 spatialValue)
        {
            _spatialValue = new Vector2(
               Mathf.Clamp(spatialValue.x, MinimumSpatialValue.x, MaximumSpatialValue.x),
               Mathf.Clamp(spatialValue.y, MinimumSpatialValue.y, MaximumSpatialValue.y)
            );
            Vector2 OutputValue = ConvertToOutputValue(_spatialValue);
            SetValueOnStateModule(OutputValue);
        }

        private void HandleStateValueChanged(Vector2 value)
        {
            // Values received from the state are always output values  
            // Convert the output value to spatial value  
            Vector2 newSpatialValue = ConvertToSpatialValue(value);

            // if (newSpatialValue == _spatialValue)  
            //     return;  

            _spatialValue = newSpatialValue;

            switch (_config.LinearAdjustableServiceConfig.AdjustmentAxis)
            {
                case SpatialAdjustmentAxis2D.XZAxis:
                    _transformToAdjust.localPosition = new Vector3(_spatialValue.x, _transformToAdjust.localPosition.y, _spatialValue.y);
                    break;
                case SpatialAdjustmentAxis2D.XYAxis:
                    _transformToAdjust.localPosition = new Vector3(_spatialValue.x, _spatialValue.y, _transformToAdjust.localPosition.z);
                    break;
            }

            _rangedAdjustable2DInteractionModule.NotifyValueChanged();
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
            // Get the vector position of the grabber in the local space of the object  
            Vector3 localGrabPosition = _transformToAdjust.InverseTransfromPoint(grabberPosition);
            Vector2 adjustment = Vector2.zero;

            // Get the grabber value X/Y based on the adjustment axis X/Y  
            switch (_config.LinearAdjustableServiceConfig.AdjustmentAxis)
            {
                case SpatialAdjustmentAxis2D.XZAxis:
                    adjustment = new Vector2(localGrabPosition.x, localGrabPosition.z); // Allow movement along X and Z
                    break;
                case SpatialAdjustmentAxis2D.XYAxis:
                    adjustment = new Vector2(localGrabPosition.x, localGrabPosition.y); // Allow movement along X and Y
                    break;
            }

            // Clamp the adjustment to the defined spatial range  
            adjustment = new Vector2(
                Mathf.Clamp(adjustment.x, MinimumSpatialValue.x, MaximumSpatialValue.x),
                Mathf.Clamp(adjustment.y, MinimumSpatialValue.y, MaximumSpatialValue.y)
            );

            _spatialValue = adjustment;
            Vector2 outputValue = ConvertToOutputValue(_spatialValue);
            SetValueOnStateModule(outputValue, _grabbableStateModule.MostRecentInteractingClientID.Value);
        }

        /* MAKE SURE ANY SPATIAL VALUE IS CONVERTED TO OUTPUT VALUE BEFORE SETTING IT TO THE STATE MODULE
        ALWAYS CALL THIS VALUE TO SET THE VALUE TO THE STATE MODULE, it calculates it automatically based on if it is discrete and continuous
        and sets it to tthe state module */
        private void SetValueOnStateModule(Vector2 value, ushort clientID = ushort.MaxValue)
        {
            //UnityEngine.Debug.Log($"value = {value}, Adjustable2DStateModule.OutputValue = {_Adjustable2DStateModule.OutputValue}");

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

        private void SetValueByStep(Vector2 value, ushort clientID)
        {
            value = new Vector2(
               Mathf.Clamp(value.x, _adjustable2DStateModule.MinimumOutputValue.x, _adjustable2DStateModule.MaximumOutputValue.x),
               Mathf.Clamp(value.y, _adjustable2DStateModule.MinimumOutputValue.y, _adjustable2DStateModule.MaximumOutputValue.y)
            );

            // Get the size between each step based on the number of values provided and the index of the step
            Vector2 stepSize = new Vector2(
               (_adjustable2DStateModule.MaximumOutputValue.x - _adjustable2DStateModule.MinimumOutputValue.x) / (_numberOfValues - 1),
               (_adjustable2DStateModule.MaximumOutputValue.y - _adjustable2DStateModule.MinimumOutputValue.y) / (_numberOfValues - 1)
            );

            Vector2 stepIndex = new Vector2(
               Mathf.RoundToInt((value.x - _adjustable2DStateModule.MinimumOutputValue.x) / stepSize.x),
               Mathf.RoundToInt((value.y - _adjustable2DStateModule.MinimumOutputValue.y) / stepSize.y)
            );

            Vector2 newValue = new Vector2(
               _adjustable2DStateModule.MinimumOutputValue.x + stepIndex.x * stepSize.x,
               _adjustable2DStateModule.MinimumOutputValue.y + stepIndex.y * stepSize.y
            );

            _adjustable2DStateModule.SetOutputValueInternal(newValue, clientID);
        }

        private Vector2 ConvertToSpatialValue(Vector2 outputValue)
        {
            return new Vector2(
               Mathf.Lerp(_config.LinearAdjustableServiceConfig.MinimumSpatialValue.x, _config.LinearAdjustableServiceConfig.MaximumSpatialValue.x, Mathf.InverseLerp(_adjustable2DStateModule.MinimumOutputValue.x, _adjustable2DStateModule.MaximumOutputValue.x, outputValue.x)),
               Mathf.Lerp(_config.LinearAdjustableServiceConfig.MinimumSpatialValue.y, _config.LinearAdjustableServiceConfig.MaximumSpatialValue.y, Mathf.InverseLerp(_adjustable2DStateModule.MinimumOutputValue.y, _adjustable2DStateModule.MaximumOutputValue.y, outputValue.y))
            );
        }
         private Vector2 ConvertToOutputValue(Vector2 spatialValue)
        {
            return new Vector2(
                Mathf.Lerp(_adjustable2DStateModule.MinimumOutputValue.x, _adjustable2DStateModule.MaximumOutputValue.x, Mathf.InverseLerp(_config.LinearAdjustableServiceConfig.MinimumSpatialValue.x, _config.LinearAdjustableServiceConfig.MaximumSpatialValue.x, spatialValue.x)),
                Mathf.Lerp(_adjustable2DStateModule.MinimumOutputValue.y, _adjustable2DStateModule.MaximumOutputValue.y, Mathf.InverseLerp(_config.LinearAdjustableServiceConfig.MinimumSpatialValue.y, _config.LinearAdjustableServiceConfig.MaximumSpatialValue.y, spatialValue.y))
            );
        }

        public void TearDown()
        {
            _rangedAdjustable2DInteractionModule.TearDown();
            _adjustable2DStateModule.TearDown();
            _grabbableStateModule.TearDown();

            _grabbableStateModule.OnGrabConfirmed -= HandleGrabConfirmed;
            _grabbableStateModule.OnDropConfirmed -= HandleDropConfirmed;
        }
    }
}