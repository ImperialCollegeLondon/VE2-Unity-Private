using System;
using UnityEngine;
using VE2.Common;
using VE2.Core.VComponents.NonInteractableInterfaces;
using VE2.Core.VComponents.InteractableInterfaces;
using static VE2.Common.CommonSerializables;
using log4net.Util;
using System.Collections.Generic;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    public class FreeGrabbableConfig
    {
        [SerializeField, IgnoreParent] public FreeGrabbableStateConfig StateConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
        [SerializeField, IgnoreParent] public RangedInteractionConfig RangedInteractionConfig = new();
    }

    public class FreeGrabbableService
    {
        #region Interfacess
        public IFreeGrabbableStateModule StateModule => _StateModule;
        public IRangedGrabInteractionModule RangedGrabInteractionModule => _RangedGrabInteractionModule;
        #endregion

        #region Modules
        private readonly FreeGrabbableStateModule _StateModule;
        private readonly RangedGrabInteractionModule _RangedGrabInteractionModule;
        #endregion

        public bool IsGrabbed => _StateModule.IsGrabbed;

        private Rigidbody _rigidbody;
        private bool _isKinematicOnStart;
        private PhysicsConstants _physicsConstants;

        public FreeGrabbableService(List<IHandheldInteractionModule> handheldInteractions, FreeGrabbableConfig config, VE2Serializable state, string id, WorldStateModulesContainer worldStateModulesContainer, IGameObjectFindProvider gameObjectFindProvider, Rigidbody rigidbody, PhysicsConstants physicsConstants)
        {
            _StateModule = new(state, config.StateConfig, id, worldStateModulesContainer, gameObjectFindProvider);
            _rigidbody  = rigidbody;
            _isKinematicOnStart = _rigidbody.isKinematic;
            _RangedGrabInteractionModule = new(handheldInteractions, config.RangedInteractionConfig, config.GeneralInteractionConfig);
            _physicsConstants = physicsConstants;

            _RangedGrabInteractionModule.OnLocalInteractorRequestGrab += (InteractorID interactorID) => _StateModule.SetGrabbed(interactorID);
            _RangedGrabInteractionModule.OnLocalInteractorRequestDrop += (InteractorID interactorID) => _StateModule.SetDropped(interactorID);

            _StateModule.OnGrabConfirmed += HandleGrabConfirmed;
            _StateModule.OnDropConfirmed += HandleDropConfirmed;
        }

        // private void HandleLocalInteractorRequestGrab(InteractorID interactorID) =>  _StateModule.SetGrabbed(interactorID);

        // private void HandleLocalInteractorRequestDrop(InteractorID interactorID) => _StateModule.SetDropped(interactorID);
        
        private void HandleGrabConfirmed(IInteractor interactor)
        {
            _rigidbody.isKinematic = false;
            _RangedGrabInteractionModule.ConfirmGrabOnInteractor(interactor);
        }
    
        private void HandleDropConfirmed(IInteractor interactor)
        {
            _rigidbody.isKinematic = _isKinematicOnStart;
            _RangedGrabInteractionModule.ConfirmDropOnInteractor(interactor);
        } 

        public void HandleFixedUpdate()
        {
            _StateModule.HandleFixedUpdate();
            if(_StateModule.IsGrabbed)
            {
                TrackPosition(_RangedGrabInteractionModule.CurrentGrabbingGrabberTransform.position);
                TrackRotation(_RangedGrabInteractionModule.CurrentGrabbingGrabberTransform.rotation);
            }
        }

        private void TrackPosition(Vector3 targetPosition)
        {
            Vector3 directionToGrabber = targetPosition - _rigidbody.position;
            float directionToGrabberMaxVelocityMagnitudeRatio = directionToGrabber.magnitude / _physicsConstants.DefaultMaxAngularVelocity;
            if (directionToGrabberMaxVelocityMagnitudeRatio > 1)
                directionToGrabber /= directionToGrabberMaxVelocityMagnitudeRatio;
            _rigidbody.linearVelocity *= _physicsConstants.VelocityDamping;
            _rigidbody.linearVelocity += directionToGrabber / Time.fixedDeltaTime * _physicsConstants.VelocityScale;
        }

        private void TrackRotation(Quaternion targetRotation)
        {
            var rotationDelta = targetRotation * Quaternion.Inverse(_rigidbody.rotation);
            rotationDelta.ToAngleAxis(out var angleInDegrees, out var rotationAxis);
            if (angleInDegrees > 180f)
                angleInDegrees -= 360f;
            var angularVelocity = rotationAxis * (angleInDegrees * Mathf.Deg2Rad);
            _rigidbody.angularVelocity *= _physicsConstants.AngularVelocityDamping;
            _rigidbody.angularVelocity += angularVelocity / Time.fixedDeltaTime * _physicsConstants.AngularVelocityScale;
        }

        public void TearDown()
        {
            _StateModule.TearDown();

            // _RangedGrabInteractionModule.OnLocalInteractorRequestGrab -= HandleLocalInteractorRequestGrab;
            // _RangedGrabInteractionModule.OnLocalInteractorRequestDrop -= HandleLocalInteractorRequestDrop;

            _StateModule.OnGrabConfirmed -= HandleGrabConfirmed;
            _StateModule.OnDropConfirmed -= HandleDropConfirmed;
        }
    }
}
