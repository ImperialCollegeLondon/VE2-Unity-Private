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
    public enum LinearAdjustmentType
    {
        XAxis,
        YAxis,
        ZAxis
    }

    [Serializable]
    public class LinearAdjustableConfig
    {
        [SerializeField, IgnoreParent] public LinearAdjustableServiceConfig LinearAdjustableServiceConfig = new();
        [SerializeField, IgnoreParent] public AdjustableStateConfig AdjustableStateConfig = new();
        [SerializeField, IgnoreParent] public FreeGrabbableStateConfig GrabbableStateConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public RangedInteractionConfig RangedInteractionConfig = new();
        [SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
    }

    [Serializable]
    public class LinearAdjustableServiceConfig
    {
        [SerializeField] public LinearAdjustmentType AdjustmentType = LinearAdjustmentType.XAxis;
    }

    public class LinearAdjustableService
    {
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

        private readonly ITransformWrapper _transformWrapper;

        private readonly LinearAdjustmentType _adjustmentType;

        public LinearAdjustableService(ITransformWrapper transformWrapper, List<IHandheldInteractionModule> handheldInteractions, LinearAdjustableConfig config, VE2Serializable adjustableState, VE2Serializable grabbableState, string id,
            WorldStateModulesContainer worldStateModulesContainer, InteractorContainer interactorContainer)
        {
            _RangedAdjustableInteractionModule = new(transformWrapper, handheldInteractions, config.RangedInteractionConfig, config.GeneralInteractionConfig);
            
            _transformWrapper = transformWrapper;
            
            _adjustmentType = config.LinearAdjustableServiceConfig.AdjustmentType;

            _AdjustableStateModule = new(adjustableState, config.AdjustableStateConfig, $"ADJ-{id}", worldStateModulesContainer);
            OnStateValueChanged(config.AdjustableStateConfig.StartingValue);
            _FreeGrabbableStateModule = new(grabbableState, config.GrabbableStateConfig, $"FG-{id}", worldStateModulesContainer, interactorContainer, RangedAdjustableInteractionModule);

            _RangedAdjustableInteractionModule.OnLocalInteractorRequestGrab += (InteractorID interactorID) => _FreeGrabbableStateModule.SetGrabbed(interactorID);
            _RangedAdjustableInteractionModule.OnLocalInteractorRequestDrop += (InteractorID interactorID) => _FreeGrabbableStateModule.SetDropped(interactorID);

            _FreeGrabbableStateModule.OnGrabConfirmed += OnGrabConfirmed;
            _FreeGrabbableStateModule.OnDropConfirmed += OnDropConfirmed;

            _AdjustableStateModule.OnValueChangedInternal += (float value) => OnStateValueChanged(value);
        }

        private void OnGrabConfirmed()
        {

        }

        private void OnDropConfirmed()
        {

        }

        private void OnStateValueChanged(float value)
        {
            switch (_adjustmentType)
            {
                case LinearAdjustmentType.XAxis:
                    _transformWrapper.localPosition = Vector3.right * value;
                    break;
                case LinearAdjustmentType.YAxis:
                    _transformWrapper.localPosition = Vector3.up * value;
                    break;
                case LinearAdjustmentType.ZAxis:
                    _transformWrapper.localPosition = Vector3.forward * value;
                    break;
            }
        }

        public void HandleFixedUpdate()
        {
            _FreeGrabbableStateModule.HandleFixedUpdate();
            if(_FreeGrabbableStateModule.IsLocalGrabbed)
            {
                TrackPosition(_FreeGrabbableStateModule.CurrentGrabbingInteractor.GrabberTransform.position);
            }

            _AdjustableStateModule.HandleFixedUpdate();
        }

        public void TrackPosition(Vector3 grabberPosition)
        {
            Vector3 localGrabPosition = _transformWrapper.InverseTransfromPoint(grabberPosition);
            float adjustment = 0f;
            switch (_adjustmentType)
            {
                case LinearAdjustmentType.XAxis:
                    adjustment = Mathf.Clamp(localGrabPosition.x, _AdjustableStateModule.MinimumValue, _AdjustableStateModule.MaximumValue);
                    break;
                case LinearAdjustmentType.YAxis:
                    adjustment = Mathf.Clamp(localGrabPosition.y, _AdjustableStateModule.MinimumValue, _AdjustableStateModule.MaximumValue);
                    break;
                case LinearAdjustmentType.ZAxis:
                    adjustment = Mathf.Clamp(localGrabPosition.z, _AdjustableStateModule.MinimumValue, _AdjustableStateModule.MaximumValue);
                    break;
            }
            _AdjustableStateModule.Value = adjustment;
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