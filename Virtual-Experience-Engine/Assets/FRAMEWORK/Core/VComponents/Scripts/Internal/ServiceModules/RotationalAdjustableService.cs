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
    internal class RotationalAdjustableConfig
    {
        [SerializeField, IgnoreParent] public AdjustableStateConfig AdjustableStateConfig = new();
        [SerializeField, IgnoreParent] public SpatialAdjustableServiceConfig RotationalAdjustableServiceConfig = new();
        [SerializeField, IgnoreParent] public GrabbableStateConfig GrabbableStateConfig = new();

        [SpaceArea(spaceAfter: 10), SerializeField, IndentArea(-1)] public RangedAdjustableInteractionConfig RangedAdjustableInteractionConfig = new();
        [SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();

        [HideIf(nameof(MultiplayerSupportPresent), false)]
        [SerializeField, IgnoreParent] public WorldStateSyncConfig SyncConfig = new();

        private bool MultiplayerSupportPresent => VE2API.HasMultiPlayerSupport;

        //Constructor used for tests
        public RotationalAdjustableConfig(ITransformWrapper attachPointWrapper, ITransformWrapper transformToMove)
        {
            RangedAdjustableInteractionConfig.AttachPointWrapper = attachPointWrapper;
            RangedAdjustableInteractionConfig.TransformToAdjust = transformToMove;
        }
        public RotationalAdjustableConfig() {}
    }

    internal class RotationalAdjustableService
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
        public IAdjustableStateModule AdjustableStateModule => _AdjustableStateModule;
        public IGrabbableStateModule GrabbableStateModule => _FreeGrabbableStateModule;
        public IRangedAdjustableInteractionModule RangedAdjustableInteractionModule => _RangedAdjustableInteractionModule;
        #endregion

        #region Modules
        private readonly AdjustableStateModule _AdjustableStateModule;
        private readonly GrabbableStateModule _FreeGrabbableStateModule;
        private readonly RangedAdjustableInteractionModule _RangedAdjustableInteractionModule;
        #endregion

        private readonly RotationalAdjustableConfig _config;

        //gets the vector from the object to the attach point, this will serve as the starting point for any angle created
        //needs to be the attach point at the start (0 not starting position) to get the correct angle
        private Vector3 _vectorToHandle => _attachPointTransform.position - _transformToAdjust.position;

        private float _signedAngle = 0;
        private float _oldRotationalValue = 0;
        private int _numberOfRevolutions = 0;
        private int _minRevs => (int)MinimumSpatialValue / 360;
        private int _maxRevs => (int)MaximumSpatialValue / 360;

        public RotationalAdjustableService(List<IHandheldInteractionModule> handheldInteractions, RotationalAdjustableConfig config, AdjustableState adjustableState, VE2Serializable grabbableState, string id,
            IWorldStateSyncableContainer worldStateSyncableContainer, IGrabInteractablesContainer grabInteractablesContainer, HandInteractorContainer interactorContainer, IClientIDWrapper localClientIdWrapper)
        {
            _config = config;

            _RangedAdjustableInteractionModule = new(id, grabInteractablesContainer, handheldInteractions, config.RangedAdjustableInteractionConfig, config.GeneralInteractionConfig);

            //seperate modules for adjustable state and free grabbable state. Give the adjustable state module a different ID so it doesn't clash in the syncer with the grabbable state module
            //The Grabbable state module needs the same ID that is passed to the ranged adjustable interaction module, so the interactor can pull the module from the grab interactable container
            _AdjustableStateModule = new(adjustableState, config.AdjustableStateConfig, config.SyncConfig, $"ADJ-{id}", worldStateSyncableContainer, localClientIdWrapper);
            _FreeGrabbableStateModule = new(grabbableState, config.GrabbableStateConfig, config.SyncConfig, $"{id}", worldStateSyncableContainer, interactorContainer, localClientIdWrapper);

            _RangedAdjustableInteractionModule.OnLocalInteractorRequestGrab += (InteractorID interactorID) => _FreeGrabbableStateModule.SetGrabbed(interactorID);
            _RangedAdjustableInteractionModule.OnLocalInteractorRequestDrop += (InteractorID interactorID) => _FreeGrabbableStateModule.SetDropped(interactorID);

            _RangedAdjustableInteractionModule.OnScrollUp += OnScrollUp;
            _RangedAdjustableInteractionModule.OnScrollDown += OnScrollDown;

            _FreeGrabbableStateModule.OnGrabConfirmed += OnGrabConfirmed;
            _FreeGrabbableStateModule.OnDropConfirmed += OnDropConfirmed;

            _AdjustableStateModule.OnValueChangedInternal += (float value) => OnStateValueChanged(value);

            //set the initial value of the adjustable state module
            if (!adjustableState.IsInitialised)
                SetValueOnStateModule(config.AdjustableStateConfig.StartingOutputValue);
            adjustableState.IsInitialised = true;

            //get the nth revolution of the starting value
            _numberOfRevolutions = Mathf.FloorToInt(ConvertToSpatialValue(config.AdjustableStateConfig.StartingOutputValue) / 360);
            _oldRotationalValue = ConvertToSpatialValue(config.AdjustableStateConfig.StartingOutputValue) - (_numberOfRevolutions * 360);
        }

        private void OnScrollUp()
        {
            float targetValue = _AdjustableStateModule.OutputValue + _config.AdjustableStateConfig.IncrementPerScrollTick; //should this change spatial value?
            targetValue = Mathf.Clamp(targetValue, _AdjustableStateModule.MinimumOutputValue, _AdjustableStateModule.MaximumOutputValue);
            SetValueOnStateModule(targetValue);

            float spatialVal = ConvertToSpatialValue(targetValue);
            _oldRotationalValue = (spatialVal % 360 + 360) % 360; //this is to make sure the value is always positive
            _numberOfRevolutions = Mathf.FloorToInt(spatialVal / 360);
        }

        private void OnScrollDown()
        {
            float targetValue = _AdjustableStateModule.OutputValue - _config.AdjustableStateConfig.IncrementPerScrollTick; //should this change spatial value?
            targetValue = Mathf.Clamp(targetValue, _AdjustableStateModule.MinimumOutputValue, _AdjustableStateModule.MaximumOutputValue);
            SetValueOnStateModule(targetValue);

            float spatialVal = ConvertToSpatialValue(targetValue);
            _oldRotationalValue = (spatialVal % 360 + 360) % 360; //this is to make sure the value is always positive
            _numberOfRevolutions = Mathf.FloorToInt(spatialVal / 360);
        }

        private void OnGrabConfirmed(ushort id)
        {
            _oldRotationalValue = (_spatialValue % 360 + 360) % 360; //this is to make sure the value is always positive
            _numberOfRevolutions = Mathf.FloorToInt(_spatialValue / 360); //get the nth revolution of the starting value
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

            if (newSpatialValue == _spatialValue)
                return;

            _spatialValue = newSpatialValue;

            // //set revs and the old rotational value received from the state if host so the values remained synced
            // _numberOfRevolutions = Mathf.FloorToInt(_spatialValue / 360);
            // _oldRotationalValue = (_spatialValue % 360 + 360) % 360; //this is to make sure the value is always positive

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

            _RangedAdjustableInteractionModule.NotifyValueChanged();
        }

        public void HandleFixedUpdate()
        {
            _FreeGrabbableStateModule.HandleFixedUpdate();
            if (_FreeGrabbableStateModule.IsLocalGrabbed)
            {
                TrackPosition(_FreeGrabbableStateModule.CurrentGrabbingInteractor.GrabberTransformWrapper.position);
            }

            _AdjustableStateModule.HandleFixedUpdate();

            Debug.DrawLine(_transformToAdjust.position, _transformToAdjust.position + _vectorToHandle, Color.red);
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
            localDirectionToHandle = Vector3.ProjectOnPlane(_vectorToHandle, axisOfRotation);
            _signedAngle = Vector3.SignedAngle(localDirectionToHandle.normalized, localDirectionToGrabber.normalized, axisOfRotation);

            //signed angle is always between -180 and 180, we need to convert it to 0-360
            if (_signedAngle < 0)
                _signedAngle += 360;


            // if (_adjustmentProperty == SpatialAdjustmentProperty.Continuous)
            // {
            //     //dont allow the angle to go over 360 or under 0 if the state is at the maximum or minimum value
            //     if (_signedAngle > 0 && _signedAngle < 345 && _AdjustableStateModule.IsAtMaximumValue)
            //         _signedAngle = 0;
            //     else if (_signedAngle < 360 && _signedAngle > 15 && _AdjustableStateModule.IsAtMinimumValue)
            //         _signedAngle = 0;
            // }
            // else
            // {
            //     //get the size between each step based on the number of values provided and the index of the step
            //     float stepSize = (_maximumSpatialValue - _minimumSpatialValue) / _numberOfValues;

            //     //dont allow the angle to go over 360 or under 0 if the state is at the maximum or minimum value
            //     if (_signedAngle > 0 && _signedAngle < 360 - stepSize && _AdjustableStateModule.IsAtMaximumValue)
            //         _signedAngle = 0;
            //     else if (_signedAngle < 360 && _signedAngle > stepSize && _AdjustableStateModule.IsAtMinimumValue)
            //         _signedAngle = 0;
            // }

            if (_signedAngle - _oldRotationalValue < -180) //if the angle has gone over 360 increment the number of revolutions
                _numberOfRevolutions++;
            else if (_signedAngle - _oldRotationalValue > 180) //if the angle has gone under 0 decrement the number of revolutions
                _numberOfRevolutions--;

            _numberOfRevolutions = Mathf.Clamp(_numberOfRevolutions, _minRevs - 1, _maxRevs + 1);
            _oldRotationalValue = _signedAngle; //set the old rotational value to the current signed angle

            //Debug.Log($"Sending Signed Angle: {_signedAngle} | Old Rotational Value: {_oldRotationalValue} | Difference: {_signedAngle - _oldRotationalValue} | Number of Revs: {_numberOfRevolutions}");

            float angularAdjustment = _signedAngle + (_numberOfRevolutions * 360);
            angularAdjustment = Mathf.Clamp(angularAdjustment, MinimumSpatialValue, MaximumSpatialValue);

            float OutputValue = ConvertToOutputValue(angularAdjustment);
            SetValueOnStateModule(OutputValue);
        }

        /* MAKE SURE ANY SPATIAL VALUE IS CONVERTED TO OUTPUT VALUE BEFORE SETTING IT TO THE STATE MODULE
        ALWAYS CALL THIS VALUE TO SET THE VALUE TO THE STATE MODULE, it calculates it automatically based on if it is discrete and continuous
        and sets it to tthe state module */
        private void SetValueOnStateModule(float value)
        {
            if (_config.RotationalAdjustableServiceConfig.AdjustmentType == SpatialAdjustmentType.Discrete)
                SetValueByStep(value);
            else
                _AdjustableStateModule.SetOutputValue(value);
        }

        private void UpdateSteps(int steps)
        {
            _config.RotationalAdjustableServiceConfig.NumberOfDiscreteValues = steps;
            SetValueOnStateModule(_AdjustableStateModule.OutputValue);
        }

        private void SetValueByStep(float value)
        {
            value = Mathf.Clamp(value, _AdjustableStateModule.MinimumOutputValue, _AdjustableStateModule.MaximumOutputValue);

            //get the size between each step based on the number of values provided and the index of the step
            float stepSize = (_AdjustableStateModule.MaximumOutputValue - _AdjustableStateModule.MinimumOutputValue) / _numberOfValues;
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
            _FreeGrabbableStateModule.TearDown();

            _FreeGrabbableStateModule.OnGrabConfirmed -= OnGrabConfirmed;
            _FreeGrabbableStateModule.OnDropConfirmed -= OnDropConfirmed;
        }

    }
}
