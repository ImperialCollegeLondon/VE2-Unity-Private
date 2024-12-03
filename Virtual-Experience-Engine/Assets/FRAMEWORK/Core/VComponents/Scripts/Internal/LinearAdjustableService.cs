using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.NonInteractableInterfaces;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    public class LinearAdjustableConfig
    {
        [SerializeField, IgnoreParent] public AdjustableStateConfig adjustableStateConfig = new();
        [SerializeField, IgnoreParent] public FreeGrabbableStateConfig grabbableStateConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public RangedInteractionConfig RangedInteractionConfig = new();
        [SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
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

        private ITransformWrapper _transformWrapper;
        public LinearAdjustableService(Transform transform, List<IHandheldInteractionModule> handheldInteractions, LinearAdjustableConfig config, VE2Serializable adjustableState, VE2Serializable grabbableState, string id,
            WorldStateModulesContainer worldStateModulesContainer, InteractorContainer interactorContainer, ITransformWrapper transformWrapper)
        {
            _AdjustableStateModule = new(adjustableState, config.adjustableStateConfig, id, worldStateModulesContainer);
            _FreeGrabbableStateModule = new(grabbableState, config.grabbableStateConfig, id, worldStateModulesContainer, interactorContainer, RangedAdjustableInteractionModule);
            _RangedAdjustableInteractionModule = new(transform, handheldInteractions, config.RangedInteractionConfig, config.GeneralInteractionConfig);

            _transformWrapper = transformWrapper;

            _RangedAdjustableInteractionModule.OnLocalInteractorRequestGrab += (InteractorID interactorID) => _FreeGrabbableStateModule.SetGrabbed(interactorID);
            _RangedAdjustableInteractionModule.OnLocalInteractorRequestDrop += (InteractorID interactorID) => _FreeGrabbableStateModule.SetDropped(interactorID);

            _FreeGrabbableStateModule.OnGrabConfirmed += OnGrabConfirmed;
            _FreeGrabbableStateModule.OnDropConfirmed += OnDropConfirmed;

            _AdjustableStateModule.OnValueChanged += (float value) => OnStateValueChanged(value);
        }

        private void OnGrabConfirmed()
        {

        }

        private void OnDropConfirmed()
        {

        }

        private void OnStateValueChanged(float value)
        {
            _transformWrapper.localPosition = Vector3.right * value;
        }

        public void HandleFixedUpdate()
        {
            _AdjustableStateModule.HandleFixedUpdate();
            _FreeGrabbableStateModule.HandleFixedUpdate();
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