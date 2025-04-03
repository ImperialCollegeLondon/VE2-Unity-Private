using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common.TransformWrapper;
using VE2.Core.VComponents.API;
using static VE2.Core.Common.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class RotationalAdjustableConfig
    {
        [SerializeField, IgnoreParent] public SpatialAdjustableServiceConfig RotationalAdjustableServiceConfig = new();
        [SerializeField, IgnoreParent] public AdjustableStateConfig AdjustableStateConfig = new();
        [SerializeField, IgnoreParent] public GrabbableStateConfig GrabbableStateConfig = new();
        [SerializeField, IgnoreParent] public AdjustableInteractionConfig InteractionConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public RangedInteractionConfig RangedInteractionConfig = new();
        [SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
    }

    internal class RotationalAdjustableService
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
        public IGrabbableStateModule GrabbableStateModule => _FreeGrabbableStateModule;
        public IRangedAdjustableInteractionModule RangedAdjustableInteractionModule => _RangedAdjustableInteractionModule;
        #endregion

        #region Modules
        private readonly AdjustableStateModule _AdjustableStateModule;
        private readonly GrabbableStateModule _FreeGrabbableStateModule;
        private readonly RangedAdjustableInteractionModule _RangedAdjustableInteractionModule;
        #endregion

        private readonly SpatialAdjustmentProperty _adjustmentProperty;
        private readonly ITransformWrapper _transformToRotateWrapper;
        private readonly ITransformWrapper _attachPointTransform;
        private readonly SpatialAdjustmentType _adjustmentType;
        private readonly Vector3 _vectorToHandle;
        private readonly float _incrementPerScrollTick;

        private float _signedAngle = 0;
        private float _oldRotationalValue = 0;
        private int _numberOfRevolutions = 0;
        private int _minRevs => (int)_minimumSpatialValue / 360;
        private int _maxRevs => (int)_maximumSpatialValue / 360;

        public RotationalAdjustableService(ITransformWrapper transformWrapper, List<IHandheldInteractionModule> handheldInteractions, RotationalAdjustableConfig config, VE2Serializable adjustableState, VE2Serializable grabbableState, string id,
            IWorldStateSyncService worldStateSyncService, HandInteractorContainer interactorContainer)
        {
            ITransformWrapper transformToRotateWrapper = config.InteractionConfig.TransformToAdjust == null ? transformWrapper : new TransformWrapper(config.InteractionConfig.TransformToAdjust);

            //get attach point transform if it exists, if null take the transform wrapper of the object itself
            _attachPointTransform = config.InteractionConfig.AttachPoint == null ? transformToRotateWrapper : new TransformWrapper(config.InteractionConfig.AttachPoint);

            //initialize module for ranged adjustable interaction (scrolling)
            _RangedAdjustableInteractionModule = new(_attachPointTransform, handheldInteractions, config.RangedInteractionConfig, config.GeneralInteractionConfig);

            _incrementPerScrollTick = config.AdjustableStateConfig.IncrementPerScrollTick;
            _transformToRotateWrapper = transformToRotateWrapper;

            _adjustmentType = config.RotationalAdjustableServiceConfig.AdjustmentType;
            _adjustmentProperty = config.RotationalAdjustableServiceConfig.AdjustmentProperty;

            if (_adjustmentProperty == SpatialAdjustmentProperty.Discrete)
                _numberOfValues = config.RotationalAdjustableServiceConfig.NumberOfValues;

            _minimumSpatialValue = config.RotationalAdjustableServiceConfig.MinimumSpatialValue;
            _maximumSpatialValue = config.RotationalAdjustableServiceConfig.MaximumSpatialValue;

            //seperate modules for adjustable state and free grabbable state, they have a unique ID for each for the world state syncer
            _AdjustableStateModule = new(adjustableState, config.AdjustableStateConfig, $"ADJ-{id}", worldStateSyncService);
            _FreeGrabbableStateModule = new(grabbableState, config.GrabbableStateConfig, $"FG-{id}", worldStateSyncService, interactorContainer, RangedAdjustableInteractionModule);

            _RangedAdjustableInteractionModule.OnLocalInteractorRequestGrab += (InteractorID interactorID) => _FreeGrabbableStateModule.SetGrabbed(interactorID);
            _RangedAdjustableInteractionModule.OnLocalInteractorRequestDrop += (InteractorID interactorID) => _FreeGrabbableStateModule.SetDropped(interactorID);

            _RangedAdjustableInteractionModule.OnScrollUp += OnScrollUp;
            _RangedAdjustableInteractionModule.OnScrollDown += OnScrollDown;

            _FreeGrabbableStateModule.OnGrabConfirmed += OnGrabConfirmed;
            _FreeGrabbableStateModule.OnDropConfirmed += OnDropConfirmed;

            _AdjustableStateModule.OnValueChangedInternal += (float value) => OnStateValueChanged(value);

            //gets the vector from the object to the attach point, this will serve as the starting point for any angle created
            //needs to be the attafch point at the start (0 not starting position) to get the correct angle
            _vectorToHandle = _attachPointTransform.position - _transformToRotateWrapper.position;

            //set the initial value of the adjustable state module
            SetValueOnStateModule(config.AdjustableStateConfig.StartingOutputValue);

            //get the nth revolution of the starting value
            _numberOfRevolutions = Mathf.FloorToInt(ConvertToSpatialValue(config.AdjustableStateConfig.StartingOutputValue) / 360);
            _oldRotationalValue = ConvertToSpatialValue(config.AdjustableStateConfig.StartingOutputValue) - (_numberOfRevolutions * 360);
        }

        private void OnScrollUp()
        {
            float targetValue = _AdjustableStateModule.OutputValue + _incrementPerScrollTick; //should this change spatial value?
            targetValue = Mathf.Clamp(targetValue, _AdjustableStateModule.MinimumOutputValue, _AdjustableStateModule.MaximumOutputValue);
            SetValueOnStateModule(targetValue);

            float spatialVal = ConvertToSpatialValue(targetValue);
            _oldRotationalValue = (spatialVal % 360 + 360) % 360; //this is to make sure the value is always positive
            _numberOfRevolutions = Mathf.FloorToInt(spatialVal / 360);
        }

        private void OnScrollDown()
        {
            float targetValue = _AdjustableStateModule.OutputValue - _incrementPerScrollTick; //should this change spatial value?
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
            _spatialValue = Mathf.Clamp(spatialValue, _minimumSpatialValue, _maximumSpatialValue);
            float OutputValue = ConvertToOutputValue(_spatialValue);
            SetValueOnStateModule(OutputValue);
        }

        private void OnStateValueChanged(float value)
        {
            //Values received from the state are always output values
            //convert the output value to spatial value
            _spatialValue = ConvertToSpatialValue(value);

            // //set revs and the old rotational value received from the state if host so the values remained synced
            // _numberOfRevolutions = Mathf.FloorToInt(_spatialValue / 360);
            // _oldRotationalValue = (_spatialValue % 360 + 360) % 360; //this is to make sure the value is always positive

            switch (_adjustmentType)
            {
                case SpatialAdjustmentType.XAxis:
                    _transformToRotateWrapper.localRotation = Quaternion.Euler(_spatialValue, _transformToRotateWrapper.localRotation.y, _transformToRotateWrapper.localRotation.z);
                    break;
                case SpatialAdjustmentType.YAxis:
                    _transformToRotateWrapper.localRotation = Quaternion.Euler(_transformToRotateWrapper.localRotation.x, _spatialValue, _transformToRotateWrapper.localRotation.z);
                    break;
                case SpatialAdjustmentType.ZAxis:
                    _transformToRotateWrapper.localRotation = Quaternion.Euler(_transformToRotateWrapper.localRotation.x, _transformToRotateWrapper.localRotation.y, _spatialValue);
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

            Debug.DrawLine(_transformToRotateWrapper.position, _transformToRotateWrapper.position + _vectorToHandle, Color.red);
        }

        private void TrackPosition(Vector3 grabberPosition)
        {
            //get the direction from the object to the grabber
            Vector3 directionToGrabber = grabberPosition - _transformToRotateWrapper.position;
            Vector3 localDirectionToGrabber, localDirectionToHandle;
            Vector3 axisOfRotation = _transformToRotateWrapper.up;

            switch (_adjustmentType)
            {
                case SpatialAdjustmentType.XAxis:
                    axisOfRotation = _transformToRotateWrapper.right;
                    break;
                case SpatialAdjustmentType.YAxis:
                    axisOfRotation = _transformToRotateWrapper.up;
                    break;
                case SpatialAdjustmentType.ZAxis:
                    axisOfRotation = _transformToRotateWrapper.forward;
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
            angularAdjustment = Mathf.Clamp(angularAdjustment, _minimumSpatialValue, _maximumSpatialValue);

            float OutputValue = ConvertToOutputValue(angularAdjustment);
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
            _AdjustableStateModule.TearDown();
            _FreeGrabbableStateModule.TearDown();

            _FreeGrabbableStateModule.OnGrabConfirmed -= OnGrabConfirmed;
            _FreeGrabbableStateModule.OnDropConfirmed -= OnDropConfirmed;
        }

    }
}
