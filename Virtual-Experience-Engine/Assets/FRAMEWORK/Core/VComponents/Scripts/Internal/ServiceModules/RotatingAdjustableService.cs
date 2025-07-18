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
    internal class RotatingAdjustableConfig
    {
        public void OpenDocs() => Application.OpenURL("https://www.notion.so/V_RotationalAdjustable-2170e4d8ed4d800abacafa5b3f72dad7?source=copy_link");
        [EditorButton(nameof(OpenDocs), "Open Docs", PositionType = ButtonPositionType.Above)]
        [SerializeField, IgnoreParent] public AdjustableStateConfig AdjustableStateConfig = new();
        [SerializeField, IgnoreParent] public SpatialAdjustableServiceConfig RotationalAdjustableServiceConfig = new();
        [SerializeField, IgnoreParent] public GrabbableStateConfig GrabbableStateConfig = new();

        [SpaceArea(spaceAfter: 10), SerializeField, IndentArea(-1)] public RangedAdjustableInteractionConfig RangedAdjustableInteractionConfig = new();
        [SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();

        [HideIf(nameof(MultiplayerSupportPresent), false)]
        [SerializeField, IgnoreParent] public WorldStateSyncConfig SyncConfig = new();

        private bool MultiplayerSupportPresent => VE2API.HasMultiPlayerSupport;

        //Constructor used for tests
        public RotatingAdjustableConfig(ITransformWrapper attachPointWrapper, ITransformWrapper transformToMove)
        {
            RangedAdjustableInteractionConfig.AttachPointWrapper = attachPointWrapper;
            RangedAdjustableInteractionConfig.TransformToAdjust = transformToMove;
        }
        public RotatingAdjustableConfig() {}
    }

    internal class RotatingAdjustableService
    {
        private ITransformWrapper _attachPointTransform => _config.RangedAdjustableInteractionConfig.AttachPointWrapper;

        private ITransformWrapper _transformToAdjust => _config.RangedAdjustableInteractionConfig.TransformToAdjust;
        private float _spatialValue;
        public float SpatialValue { get => _spatialValue; set => SetSpatialValue(value); }
        public float MinimumSpatialValue { get => _config.RotationalAdjustableServiceConfig.MinimumSpatialValue; set => _config.RotationalAdjustableServiceConfig.MinimumSpatialValue = value; }
        public float MaximumSpatialValue { get => _config.RotationalAdjustableServiceConfig.MaximumSpatialValue; set => _config.RotationalAdjustableServiceConfig.MaximumSpatialValue = value; }
        private int _numberOfValues => _config.RotationalAdjustableServiceConfig.NumberOfDiscreteValues;
        public int NumberOfValues { get => _numberOfValues; set => UpdateSteps(value); }

        #region Interfaces
        public IAdjustableStateModule AdjustableStateModule => _adjustableStateModule;
        public IGrabbableStateModule GrabbableStateModule => _grabbableStateModule;
        public IRangedAdjustableInteractionModule RangedAdjustableInteractionModule => _rangedAdjustableInteractionModule;
        #endregion

        #region Modules
        private readonly AdjustableStateModule _adjustableStateModule;
        private readonly GrabbableStateModule _grabbableStateModule;
        private readonly RangedAdjustableInteractionModule _rangedAdjustableInteractionModule;
        #endregion

        private readonly RotatingAdjustableConfig _config;

        //gets the vector from the object to the attach point, this will serve as the starting point for any angle created
        //needs to be the attach point at the start (0 not starting position) to get the correct angle
        private Vector3 _initialVectorToHandle = Vector3.zero;

        private float _signedAngle = 0;
        private float _oldRotationalValue = 0;
        private int _numberOfRevolutions = 0;
        private int _minRevs => (int)MinimumSpatialValue / 360;
        private int _maxRevs => (int)MaximumSpatialValue / 360;

        public RotatingAdjustableService(List<IHandheldInteractionModule> handheldInteractions, RotatingAdjustableConfig config, AdjustableState adjustableState, VE2Serializable grabbableState, string id,
            IWorldStateSyncableContainer worldStateSyncableContainer, IGrabInteractablesContainer grabInteractablesContainer, HandInteractorContainer interactorContainer, IClientIDWrapper localClientIdWrapper)
        {
            _config = config;

            //needs the vector to the attachpoint at 0,0,0
            _initialVectorToHandle = _attachPointTransform.position - _transformToAdjust.position;

            _rangedAdjustableInteractionModule = new(id, grabInteractablesContainer, handheldInteractions, config.RangedAdjustableInteractionConfig, config.GeneralInteractionConfig);

            //seperate modules for adjustable state and free grabbable state. Give the adjustable state module a different ID so it doesn't clash in the syncer with the grabbable state module
            //The Grabbable state module needs the same ID that is passed to the ranged adjustable interaction module, so the interactor can pull the module from the grab interactable container
            _adjustableStateModule = new(adjustableState, config.AdjustableStateConfig, config.SyncConfig, $"ADJ-{id}", worldStateSyncableContainer, localClientIdWrapper);
            _grabbableStateModule = new(grabbableState, config.GrabbableStateConfig, config.SyncConfig, $"{id}", worldStateSyncableContainer, grabInteractablesContainer, interactorContainer, localClientIdWrapper);

            _rangedAdjustableInteractionModule.OnLocalInteractorRequestGrab += (InteractorID interactorID) => _grabbableStateModule.SetGrabbed(interactorID);
            _rangedAdjustableInteractionModule.OnLocalInteractorRequestDrop += (InteractorID interactorID) => _grabbableStateModule.SetDropped(interactorID);

            _rangedAdjustableInteractionModule.OnScrollUp += HandleScrollUp;
            _rangedAdjustableInteractionModule.OnScrollDown += HandleScrollDown;

            _grabbableStateModule.OnGrabConfirmed += HandleGrabConfirmed;
            _grabbableStateModule.OnDropConfirmed += HandleDropConfirmed;

            _adjustableStateModule.OnValueChangedInternal += (float value) => HandleStateValueChanged(value);

            //get the nth revolution of the starting value
            _numberOfRevolutions = Mathf.FloorToInt(ConvertToSpatialValue(config.AdjustableStateConfig.StartingOutputValue) / 360);
            _oldRotationalValue = ConvertToSpatialValue(config.AdjustableStateConfig.StartingOutputValue) - (_numberOfRevolutions * 360);
        }

        public void HandleStart() => _adjustableStateModule.InitializeStateWithStartingValue();

        private void HandleScrollUp(ushort clientID)
        {
            float scrollMultiplier = _config.RotationalAdjustableServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete ? (_adjustableStateModule.MaximumOutputValue - _adjustableStateModule.MinimumOutputValue) / (_numberOfValues - 1) : _config.AdjustableStateConfig.IncrementPerScrollTick;
            float targetValue = _adjustableStateModule.OutputValue + scrollMultiplier; //should this change spatial value?
            targetValue = Mathf.Clamp(targetValue, _adjustableStateModule.MinimumOutputValue, _adjustableStateModule.MaximumOutputValue);
            SetValueOnStateModule(targetValue, clientID);

            float spatialVal = ConvertToSpatialValue(targetValue);
            _oldRotationalValue = (spatialVal % 360 + 360) % 360; //this is to make sure the value is always positive
            _numberOfRevolutions = Mathf.FloorToInt(spatialVal / 360);
        }

        private void HandleScrollDown(ushort clientID)
        {
            float scrollMultiplier = _config.RotationalAdjustableServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete ? (_adjustableStateModule.MaximumOutputValue - _adjustableStateModule.MinimumOutputValue) / (_numberOfValues - 1) : _config.AdjustableStateConfig.IncrementPerScrollTick;
            float targetValue = _adjustableStateModule.OutputValue - scrollMultiplier; //should this change spatial value?
            targetValue = Mathf.Clamp(targetValue, _adjustableStateModule.MinimumOutputValue, _adjustableStateModule.MaximumOutputValue);
            SetValueOnStateModule(targetValue, clientID);

            float spatialVal = ConvertToSpatialValue(targetValue);
            _oldRotationalValue = (spatialVal % 360 + 360) % 360; //this is to make sure the value is always positive
            _numberOfRevolutions = Mathf.FloorToInt(spatialVal / 360);
        }

        private void HandleGrabConfirmed(ushort id)
        {
            _oldRotationalValue = (_spatialValue % 360 + 360) % 360; //this is to make sure the value is always positive
            _numberOfRevolutions = Mathf.FloorToInt(_spatialValue / 360); //get the nth revolution of the starting value
        }

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

            _spatialValue = newSpatialValue;

            switch (_config.RotationalAdjustableServiceConfig.AdjustmentAxis)
            {
                case SpatialAdjustmentAxis.XAxis:
                    _transformToAdjust.localRotation = Quaternion.Euler(_spatialValue, _transformToAdjust.localRotation.y, _transformToAdjust.localRotation.z);
                    break;
                case SpatialAdjustmentAxis.YAxis:
                    _transformToAdjust.localRotation = Quaternion.Euler(_transformToAdjust.localRotation.x, _spatialValue, _transformToAdjust.localRotation.z);
                    break;
                case SpatialAdjustmentAxis.ZAxis:
                    _transformToAdjust.localRotation = Quaternion.Euler(_transformToAdjust.localRotation.x, _transformToAdjust.localRotation.y, _spatialValue);
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

            Debug.DrawLine(_transformToAdjust.position, _transformToAdjust.position + _initialVectorToHandle, Color.red);
        }

        private void TrackPosition(Vector3 grabberPosition)
        {
            //get the direction from the object to the grabber
            Vector3 directionToGrabber = grabberPosition - _transformToAdjust.position;
            Vector3 localDirectionToGrabber, localDirectionToHandle;
            Vector3 axisOfRotation = _transformToAdjust.up;

            switch (_config.RotationalAdjustableServiceConfig.AdjustmentAxis)
            {
                case SpatialAdjustmentAxis.XAxis:
                    axisOfRotation = _transformToAdjust.right;
                    break;
                case SpatialAdjustmentAxis.YAxis:
                    axisOfRotation = _transformToAdjust.up;
                    break;
                case SpatialAdjustmentAxis.ZAxis:
                    axisOfRotation = _transformToAdjust.forward;
                    break;
            }

            //project the direction to the grabber and the vector to the handle to be local to the plane of the axis of rotation (in this case the transfor wrappper, the object itself)
            //once thats done take the angle between the vectors relative to the plane it is projected to
            localDirectionToGrabber = Vector3.ProjectOnPlane(directionToGrabber, axisOfRotation);
            localDirectionToHandle = Vector3.ProjectOnPlane(_initialVectorToHandle, axisOfRotation);
            _signedAngle = Vector3.SignedAngle(localDirectionToHandle.normalized, localDirectionToGrabber.normalized, axisOfRotation);

            //signed angle is always between -180 and 180, we need to convert it to 0-360
            if (_signedAngle < 0)
                _signedAngle += 360;

            if (_signedAngle - _oldRotationalValue < -180) //if the angle has gone over 360 increment the number of revolutions
                _numberOfRevolutions++;
            else if (_signedAngle - _oldRotationalValue > 180) //if the angle has gone under 0 decrement the number of revolutions
                _numberOfRevolutions--;

            _numberOfRevolutions = Mathf.Clamp(_numberOfRevolutions, _minRevs - 1, _maxRevs + 1);
            _oldRotationalValue = _signedAngle; //set the old rotational value to the current signed angle

            float angularAdjustment = _signedAngle + (_numberOfRevolutions * 360);
            angularAdjustment = Mathf.Clamp(angularAdjustment, MinimumSpatialValue, MaximumSpatialValue);

            float OutputValue = ConvertToOutputValue(angularAdjustment);
            SetValueOnStateModule(OutputValue, _grabbableStateModule.MostRecentInteractingClientID.Value);
        }

        /* MAKE SURE ANY SPATIAL VALUE IS CONVERTED TO OUTPUT VALUE BEFORE SETTING IT TO THE STATE MODULE
        ALWAYS CALL THIS VALUE TO SET THE VALUE TO THE STATE MODULE, it calculates it automatically based on if it is discrete and continuous
        and sets it to tthe state module */
        private void SetValueOnStateModule(float value, ushort clientID = ushort.MaxValue)
        {
            if (_config.RotationalAdjustableServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete)
                SetValueByStep(value, clientID);
            else
                _adjustableStateModule.SetOutputValueInternal(value, clientID);
        }

        private void UpdateSteps(int steps)
        {
            _config.RotationalAdjustableServiceConfig.NumberOfDiscreteValues = steps;

            //Refresh value now that number of steps has changed
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
