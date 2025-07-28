using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Shared;
using static VE2.Common.Shared.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class Rotating2DAdjustableConfig
    {
        public void OpenDocs() => Application.OpenURL("https://www.notion.so/V_RotationalAdjustable-2170e4d8ed4d800abacafa5b3f72dad7?source=copy_link");
        [EditorButton(nameof(OpenDocs), "Open Docs", PositionType = ButtonPositionType.Above)]
        [SerializeField, IgnoreParent] public Adjustable2DStateConfig Adjustable2DStateConfig = new();
        [SerializeField, IgnoreParent] public SpatialAdjustable2DServiceConfig RotationalAdjustable2DServiceConfig = new();
        [SerializeField, IgnoreParent] public GrabbableStateConfig GrabbableStateConfig = new();

        [SpaceArea(spaceAfter: 10), SerializeField, IndentArea(-1)] public RangedAdjustableInteractionConfig RangedAdjustableInteractionConfig = new();
        [SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();

        [HideIf(nameof(MultiplayerSupportPresent), false)]
        [SerializeField, IgnoreParent] public WorldStateSyncConfig SyncConfig = new();

        private bool MultiplayerSupportPresent => VE2API.HasMultiPlayerSupport;

        //Constructor used for tests
        public Rotating2DAdjustableConfig(ITransformWrapper attachPointWrapper, ITransformWrapper transformToMove)
        {
            RangedAdjustableInteractionConfig.AttachPointWrapper = attachPointWrapper;
            RangedAdjustableInteractionConfig.TransformToAdjust = transformToMove;
        }
        public Rotating2DAdjustableConfig() { }
    }

    internal class Rotating2DAdjustableService
    {
        private ITransformWrapper _attachPointTransform => _config.RangedAdjustableInteractionConfig.AttachPointWrapper;

        private ITransformWrapper _transformToAdjust => _config.RangedAdjustableInteractionConfig.TransformToAdjust;
        private Vector2 _spatialValue;
        public Vector2 SpatialValue { get => _spatialValue; set => SetSpatialValue(value); }
        public Vector2 MinimumSpatialValue { get; set; /*=> _config.RotationalAdjustable2DServiceConfig.MinimumSpatialValue; set => _config.RotationalAdjustable2DServiceConfig.MinimumSpatialValue = value;*/ }
        public Vector2 MaximumSpatialValue { get; set; /*=> _config.RotationalAdjustable2DServiceConfig.MaximumSpatialValue; set => _config.RotationalAdjustable2DServiceConfig.MaximumSpatialValue = value;*/ }
        private int _numberOfValues => _config.RotationalAdjustable2DServiceConfig.NumberOfDiscreteValues;
        public int NumberOfValues { get => _numberOfValues; set => UpdateSteps(value); }

        #region Interfaces
        public IAdjustable2DStateModule Adjustable2DStateModule => _adjustable2DStateModule;
        public IGrabbableStateModule GrabbableStateModule => _grabbableStateModule;
        public IRangedAdjustableInteractionModule RangedAdjustableInteractionModule => _rangedAdjustableInteractionModule;
        #endregion

        #region Modules
        private readonly Adjustable2DStateModule _adjustable2DStateModule;
        private readonly GrabbableStateModule _grabbableStateModule;
        private readonly RangedAdjustableInteractionModule _rangedAdjustableInteractionModule;
        #endregion

        private readonly Rotating2DAdjustableConfig _config;

        //gets the vector from the object to the attach point, this will serve as the starting point for any angle created
        //needs to be the attach point at the start (0 not starting position) to get the correct angle
        private Vector3 _initialVectorToHandle = Vector3.zero;

        private float _signedAngle = 0;
        private Vector2 _oldRotationalValue = new Vector2(0,0);
        private Vector2Int _numberOfRevolutions = new Vector2Int(0, 0);
        private Vector2Int _minRevs => new Vector2Int(Mathf.FloorToInt(MinimumSpatialValue.x / 360), Mathf.FloorToInt(MinimumSpatialValue.y / 360));
        private Vector2Int _maxRevs => new Vector2Int(Mathf.FloorToInt(MaximumSpatialValue.x / 360), Mathf.FloorToInt(MaximumSpatialValue.y / 360));

        public Rotating2DAdjustableService(List<IHandheldInteractionModule> handheldInteractions, Rotating2DAdjustableConfig config, Adjustable2DState adjustableState, VE2Serializable grabbableState, string id,
            IWorldStateSyncableContainer worldStateSyncableContainer, IGrabInteractablesContainer grabInteractablesContainer, HandInteractorContainer interactorContainer, IClientIDWrapper localClientIdWrapper)
        {
            _config = config;

            //needs the vector to the attachpoint at 0,0,0
            _initialVectorToHandle = _attachPointTransform.position - _transformToAdjust.position;

            _rangedAdjustableInteractionModule = new(id, grabInteractablesContainer, handheldInteractions, config.RangedAdjustableInteractionConfig, config.GeneralInteractionConfig);

            //seperate modules for adjustable state and free grabbable state. Give the adjustable state module a different ID so it doesn't clash in the syncer with the grabbable state module
            //The Grabbable state module needs the same ID that is passed to the ranged adjustable interaction module, so the interactor can pull the module from the grab interactable container
            _adjustable2DStateModule = new(adjustableState, config.Adjustable2DStateConfig, config.SyncConfig, $"ADJ-{id}", worldStateSyncableContainer, localClientIdWrapper);
            _grabbableStateModule = new(grabbableState, config.GrabbableStateConfig, config.SyncConfig, $"{id}", worldStateSyncableContainer, grabInteractablesContainer, interactorContainer, localClientIdWrapper);

            _rangedAdjustableInteractionModule.OnLocalInteractorRequestGrab += (InteractorID interactorID) => _grabbableStateModule.SetGrabbed(interactorID);
            _rangedAdjustableInteractionModule.OnLocalInteractorRequestDrop += (InteractorID interactorID) => _grabbableStateModule.SetDropped(interactorID);

            _rangedAdjustableInteractionModule.OnScrollUp += HandleScrollUp;
            _rangedAdjustableInteractionModule.OnScrollDown += HandleScrollDown;

            _grabbableStateModule.OnGrabConfirmed += HandleGrabConfirmed;
            _grabbableStateModule.OnDropConfirmed += HandleDropConfirmed;

            _adjustable2DStateModule.OnValueChangedInternal += (Vector2 value) => HandleStateValueChanged(value);

            // Adjusted code to handle Vector2 for revolutions and rotational values
            _numberOfRevolutions = new Vector2Int(
               Mathf.FloorToInt(ConvertToSpatialValue(config.Adjustable2DStateConfig.StartingOutputValue).x / 360),
               Mathf.FloorToInt(ConvertToSpatialValue(config.Adjustable2DStateConfig.StartingOutputValue).y / 360)
            );

            _oldRotationalValue = new Vector2(
               ConvertToSpatialValue(config.Adjustable2DStateConfig.StartingOutputValue).x - (_numberOfRevolutions.x * 360),
               ConvertToSpatialValue(config.Adjustable2DStateConfig.StartingOutputValue).y - (_numberOfRevolutions.y * 360)
            );
        }

        public void HandleStart() => _adjustable2DStateModule.InitializeStateWithStartingValue();

        private void HandleScrollUp(ushort clientID)
        {
            Vector2 scrollMultiplier = _config.RotationalAdjustable2DServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete
                ? (_adjustable2DStateModule.MaximumOutputValue - _adjustable2DStateModule.MinimumOutputValue) / (_numberOfValues - 1)
                : new Vector2(_config.Adjustable2DStateConfig.IncrementPerScrollTick, _config.Adjustable2DStateConfig.IncrementPerScrollTick);

            Vector2 targetValue = _adjustable2DStateModule.OutputValue + scrollMultiplier;
            targetValue = new Vector2(
                Mathf.Clamp(targetValue.x, _adjustable2DStateModule.MinimumOutputValue.x, _adjustable2DStateModule.MaximumOutputValue.x),
                Mathf.Clamp(targetValue.y, _adjustable2DStateModule.MinimumOutputValue.y, _adjustable2DStateModule.MaximumOutputValue.y)
            );

            SetValueOnStateModule(targetValue, clientID);

            Vector2 spatialVal = ConvertToSpatialValue(targetValue);
            _oldRotationalValue = new Vector2(
                (spatialVal.x % 360 + 360) % 360,
                (spatialVal.y % 360 + 360) % 360
            );

            _numberOfRevolutions = new Vector2Int(
                Mathf.FloorToInt(spatialVal.x / 360),
                Mathf.FloorToInt(spatialVal.y / 360)
            );
        }

        private void HandleScrollDown(ushort clientID)
        {
            Vector2 scrollMultiplier = _config.RotationalAdjustable2DServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete
                ? (_adjustable2DStateModule.MaximumOutputValue - _adjustable2DStateModule.MinimumOutputValue) / (_numberOfValues - 1)
                : new Vector2(_config.Adjustable2DStateConfig.IncrementPerScrollTick, _config.Adjustable2DStateConfig.IncrementPerScrollTick);

            Vector2 targetValue = _adjustable2DStateModule.OutputValue - scrollMultiplier;
            targetValue = new Vector2(
                Mathf.Clamp(targetValue.x, _adjustable2DStateModule.MinimumOutputValue.x, _adjustable2DStateModule.MaximumOutputValue.x),
                Mathf.Clamp(targetValue.y, _adjustable2DStateModule.MinimumOutputValue.y, _adjustable2DStateModule.MaximumOutputValue.y)
            );

            SetValueOnStateModule(targetValue, clientID);

            Vector2 spatialVal = ConvertToSpatialValue(targetValue);
            _oldRotationalValue = new Vector2(
                (spatialVal.x % 360 + 360) % 360,
                (spatialVal.y % 360 + 360) % 360
            );

            _numberOfRevolutions = new Vector2Int(
                Mathf.FloorToInt(spatialVal.x / 360),
                Mathf.FloorToInt(spatialVal.y / 360)
            );
        }

        private void HandleScrollLeft(ushort clientID)
        {
            Vector2 scrollMultiplier = new Vector2(_config.Adjustable2DStateConfig.IncrementPerScrollTick, 0);

            Vector2 targetValue = _adjustable2DStateModule.OutputValue - scrollMultiplier;
            targetValue = new Vector2(
                Mathf.Clamp(targetValue.x, _adjustable2DStateModule.MinimumOutputValue.x, _adjustable2DStateModule.MaximumOutputValue.x),
                _adjustable2DStateModule.OutputValue.y
            );

            SetValueOnStateModule(targetValue, clientID);
        }

        private void HandleScrollRight(ushort clientID)
        {
            Vector2 scrollMultiplier = new Vector2(_config.Adjustable2DStateConfig.IncrementPerScrollTick, 0);

            Vector2 targetValue = _adjustable2DStateModule.OutputValue + scrollMultiplier;
            targetValue = new Vector2(
                Mathf.Clamp(targetValue.x, _adjustable2DStateModule.MinimumOutputValue.x, _adjustable2DStateModule.MaximumOutputValue.x),
                _adjustable2DStateModule.OutputValue.y
            );

            SetValueOnStateModule(targetValue, clientID);
        }

        private void HandleGrabConfirmed(ushort id)
        {
            _oldRotationalValue = new Vector2(
                (_spatialValue.x % 360 + 360) % 360, // Ensure positive value for X  
                (_spatialValue.y % 360 + 360) % 360  // Ensure positive value for Y  
            );

            _numberOfRevolutions = new Vector2Int(
                Mathf.FloorToInt(_spatialValue.x / 360), // Calculate revolutions for X  
                Mathf.FloorToInt(_spatialValue.y / 360)  // Calculate revolutions for Y  
            );
        }

        private void HandleDropConfirmed(ushort id) { }

        private void SetSpatialValue(Vector2 spatialValue)
        {
            _spatialValue = new Vector2(
                Mathf.Clamp(spatialValue.x, MinimumSpatialValue.x, MaximumSpatialValue.x),
                Mathf.Clamp(spatialValue.y, MinimumSpatialValue.y, MaximumSpatialValue.y)
            );
            Vector2 outputValue = ConvertToOutputValue(_spatialValue);
            SetValueOnStateModule(outputValue);
        }

        private void HandleStateValueChanged(Vector2 value)
        {
            // Convert the output value to spatial value  
            Vector2 newSpatialValue = ConvertToSpatialValue(value);
            _spatialValue = newSpatialValue;

            // Adjust rotation based on the specified adjustment axis  
            switch (_config.RotationalAdjustable2DServiceConfig.AdjustmentAxis)
            {
                case SpatialAdjustmentAxis2D.XYAxis:
                    _transformToAdjust.localRotation = Quaternion.Euler(_spatialValue.y, _spatialValue.x, _transformToAdjust.localRotation.z );
                    break;
                case SpatialAdjustmentAxis2D.XZAxis:
                    _transformToAdjust.localRotation = Quaternion.Euler(_spatialValue.x, _transformToAdjust.localRotation.y, _spatialValue.y);
                    break;
            }

            // Notify the interaction module about the value change  
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

            Debug.DrawLine(_transformToAdjust.position, _transformToAdjust.position + _initialVectorToHandle, Color.red);
        }

        private void TrackPosition(Vector3 grabberPosition)
        {
            // Get the direction from the object to the grabber  
            Vector3 directionToGrabber = grabberPosition - _transformToAdjust.position;
            Vector3 localDirectionToGrabber, localDirectionToHandle;
            Vector3 axisOfRotationX = _transformToAdjust.right;
            Vector3 axisOfRotationY = _transformToAdjust.up;

            // Determine the axes of rotation based on the adjustment axis  
            switch (_config.RotationalAdjustable2DServiceConfig.AdjustmentAxis)
            {
                case SpatialAdjustmentAxis2D.XYAxis:
                    axisOfRotationX = _transformToAdjust.right;
                    axisOfRotationY = _transformToAdjust.up;
                    break;
                case SpatialAdjustmentAxis2D.XZAxis:
                    axisOfRotationX = _transformToAdjust.right;
                    axisOfRotationY = _transformToAdjust.forward;
                    break;
            }

            // Project the direction to the grabber and the vector to the handle to be local to the plane of the axes of rotation
            localDirectionToGrabber = Vector3.ProjectOnPlane(directionToGrabber, axisOfRotationX);
            localDirectionToHandle = Vector3.ProjectOnPlane(_initialVectorToHandle, axisOfRotationX);
            float signedAngleX = Vector3.SignedAngle(localDirectionToHandle.normalized, localDirectionToGrabber.normalized, axisOfRotationX);

            localDirectionToGrabber = Vector3.ProjectOnPlane(directionToGrabber, axisOfRotationY);
            localDirectionToHandle = Vector3.ProjectOnPlane(_initialVectorToHandle, axisOfRotationY);
            float signedAngleY = Vector3.SignedAngle(localDirectionToHandle.normalized, localDirectionToGrabber.normalized, axisOfRotationY);

            // Normalize angles to 0-360 range
            if (signedAngleX < 0) signedAngleX += 360;
            if (signedAngleY < 0) signedAngleY += 360;

            // Update revolutions for X-axis
            if (signedAngleX - _oldRotationalValue.x < -180)
                _numberOfRevolutions.x++;
            else if (signedAngleX - _oldRotationalValue.x > 180)
                _numberOfRevolutions.x--;

            // Update revolutions for Y-axis
            if (signedAngleY - _oldRotationalValue.y < -180)
                _numberOfRevolutions.y++;
            else if (signedAngleY - _oldRotationalValue.y > 180)
                _numberOfRevolutions.y--;

            // Clamp revolutions and calculate angular adjustments
            _numberOfRevolutions = new Vector2Int(
                Mathf.Clamp(_numberOfRevolutions.x, _minRevs.x - 1, _maxRevs.x + 1),
                Mathf.Clamp(_numberOfRevolutions.y, _minRevs.y - 1, _maxRevs.y + 1)
            );

            _oldRotationalValue = new Vector2(signedAngleX, signedAngleY);

            Vector2 angularAdjustment = new Vector2(
                Mathf.Clamp(signedAngleX + (_numberOfRevolutions.x * 360), MinimumSpatialValue.x, MaximumSpatialValue.x),
                Mathf.Clamp(signedAngleY + (_numberOfRevolutions.y * 360), MinimumSpatialValue.y, MaximumSpatialValue.y)
            );

            // Convert to output value and set on state module
            Vector2 outputValue = ConvertToOutputValue(angularAdjustment);
            SetValueOnStateModule(outputValue, _grabbableStateModule.MostRecentInteractingClientID.Value);
        }

        /* MAKE SURE ANY SPATIAL VALUE IS CONVERTED TO OUTPUT VALUE BEFORE SETTING IT TO THE STATE MODULE
        ALWAYS CALL THIS VALUE TO SET THE VALUE TO THE STATE MODULE, it calculates it automatically based on if it is discrete and continuous
        and sets it to tthe state module */
        private void SetValueOnStateModule(Vector2 value, ushort clientID = ushort.MaxValue)
        {
            if (_config.RotationalAdjustable2DServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete)
                SetValueByStep(value, clientID);
            else
                _adjustable2DStateModule.SetOutputValueInternal(value, clientID);
        }

        private void UpdateSteps(int steps)
        {
            _config.RotationalAdjustable2DServiceConfig.NumberOfDiscreteValues = steps;

            //Refresh value now that number of steps has changed
            SetValueOnStateModule(_adjustable2DStateModule.OutputValue);
        }

        private void SetValueByStep(Vector2 value, ushort clientID)
        {
            value.x = Mathf.Clamp(value.x, _adjustable2DStateModule.MinimumOutputValue.x, _adjustable2DStateModule.MaximumOutputValue.x);
            value.y = Mathf.Clamp(value.y, _adjustable2DStateModule.MinimumOutputValue.y, _adjustable2DStateModule.MaximumOutputValue.y);

            // Calculate step size and index for X and Y components  
            float stepSizeX = (_adjustable2DStateModule.MaximumOutputValue.x - _adjustable2DStateModule.MinimumOutputValue.x) / (_numberOfValues - 1);
            float stepSizeY = (_adjustable2DStateModule.MaximumOutputValue.y - _adjustable2DStateModule.MinimumOutputValue.y) / (_numberOfValues - 1);

            int stepIndexX = Mathf.RoundToInt((value.x - _adjustable2DStateModule.MinimumOutputValue.x) / stepSizeX);
            int stepIndexY = Mathf.RoundToInt((value.y - _adjustable2DStateModule.MinimumOutputValue.y) / stepSizeY);

            // Calculate new values for X and Y components  
            float newValueX = _adjustable2DStateModule.MinimumOutputValue.x + stepIndexX * stepSizeX;
            float newValueY = _adjustable2DStateModule.MinimumOutputValue.y + stepIndexY * stepSizeY;

            // Set the new Vector2 value to the state module  
            _adjustable2DStateModule.SetOutputValueInternal(new Vector2(newValueX, newValueY), clientID);
        }

        private Vector2 ConvertToSpatialValue(Vector2 outputValue)
        {
            return new Vector2(
                Mathf.Lerp(MinimumSpatialValue.x, MaximumSpatialValue.x, Mathf.InverseLerp(_adjustable2DStateModule.MinimumOutputValue.x, _adjustable2DStateModule.MaximumOutputValue.x, outputValue.x)),
                Mathf.Lerp(MinimumSpatialValue.y, MaximumSpatialValue.y, Mathf.InverseLerp(_adjustable2DStateModule.MinimumOutputValue.y, _adjustable2DStateModule.MaximumOutputValue.y, outputValue.y))
            );
        }

        private Vector2 ConvertToOutputValue(Vector2 spatialValue)
        {
            return new Vector2(
                Mathf.Lerp(_adjustable2DStateModule.MinimumOutputValue.x, _adjustable2DStateModule.MaximumOutputValue.x, Mathf.InverseLerp(MinimumSpatialValue.x, MaximumSpatialValue.x, spatialValue.x)),
                Mathf.Lerp(_adjustable2DStateModule.MinimumOutputValue.y, _adjustable2DStateModule.MaximumOutputValue.y, Mathf.InverseLerp(MinimumSpatialValue.y, MaximumSpatialValue.y, spatialValue.y))
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
