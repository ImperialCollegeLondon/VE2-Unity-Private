using System;
using UnityEngine;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using static VE2.Core.Common.CommonSerializables;
using VE2.Common.TransformWrapper;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class FreeGrabbableConfig
    {
        [SerializeField, IgnoreParent] public GrabbableStateConfig StateConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
        [SerializeField, IgnoreParent] public RangedInteractionConfig RangedInteractionConfig = new();
    }

    internal class FreeGrabbableService
    {
        #region Interfacess
        public IGrabbableStateModule StateModule => _StateModule;
        public IRangedFreeGrabInteractionModule RangedGrabInteractionModule => _RangedGrabInteractionModule;
        #endregion

        #region Modules
        private readonly GrabbableStateModule _StateModule;
        private readonly RangedFreeGrabInteractionModule _RangedGrabInteractionModule;
        #endregion

        private IRigidbodyWrapper _rigidbody;
        private ITransformWrapper _transform;
        private bool _isKinematicOnStart;
        private PhysicsConstants _physicsConstants;

        public FreeGrabbableService(List<IHandheldInteractionModule> handheldInteractions, FreeGrabbableConfig config, VE2Serializable state, string id, 
            IWorldStateSyncService worldStateSyncService, HandInteractorContainer interactorContainer, 
            IRigidbodyWrapper rigidbody, PhysicsConstants physicsConstants)
        {
            _RangedGrabInteractionModule = new(handheldInteractions, config.RangedInteractionConfig, config.GeneralInteractionConfig);
            _StateModule = new(state, config.StateConfig, id, worldStateSyncService, interactorContainer, RangedGrabInteractionModule);
            
            _rigidbody  = rigidbody;
            _transform = config.StateConfig.AttachPoint == null ? new TransformWrapper(_rigidbody.transform) : new TransformWrapper(config.StateConfig.AttachPoint);
            _physicsConstants = physicsConstants;
            _isKinematicOnStart = _rigidbody.isKinematic;

            _RangedGrabInteractionModule.OnLocalInteractorRequestGrab += (InteractorID interactorID) => _StateModule.SetGrabbed(interactorID);
            _RangedGrabInteractionModule.OnLocalInteractorRequestDrop += (InteractorID interactorID) => _StateModule.SetDropped(interactorID);
            _RangedGrabInteractionModule.OnGrabDeltaApplied += ApplyDeltaWhenGrabbed;

            _StateModule.OnGrabConfirmed += HandleGrabConfirmed;
            _StateModule.OnDropConfirmed += HandleDropConfirmed;
        }

        // private void HandleLocalInteractorRequestGrab(InteractorID interactorID) =>  _StateModule.SetGrabbed(interactorID);

        // private void HandleLocalInteractorRequestDrop(InteractorID interactorID) => _StateModule.SetDropped(interactorID);
        
        private void ApplyDeltaWhenGrabbed(Vector3 deltaPosition, Quaternion deltaRotation)
        {
            Debug.Log("Applying delta when grabbed");   
            _rigidbody.isKinematic = true;

            _rigidbody.position += deltaPosition;
            _rigidbody.rotation *= deltaRotation;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.isKinematic = false;


        }
        private void HandleGrabConfirmed()
        {
            _rigidbody.isKinematic = false;
        }
    
        private void HandleDropConfirmed()
        {
            _rigidbody.isKinematic = _isKinematicOnStart;
        } 

        public void HandleFixedUpdate()
        {
            _StateModule.HandleFixedUpdate();
            if(_StateModule.IsGrabbed)
            {
                TrackPosition(_StateModule.CurrentGrabbingInteractor.GrabberTransform.position);
                TrackRotation(_StateModule.CurrentGrabbingInteractor.GrabberTransform.rotation);
            }
        }

        private void TrackPosition(Vector3 targetPosition)
        {
            Vector3 directionToGrabber = targetPosition - _transform.position;
            float directionToGrabberMaxVelocityMagnitudeRatio = directionToGrabber.magnitude / _physicsConstants.DefaultMaxAngularVelocity;
            if (directionToGrabberMaxVelocityMagnitudeRatio > 1)
                directionToGrabber /= directionToGrabberMaxVelocityMagnitudeRatio;
            _rigidbody.linearVelocity *= _physicsConstants.VelocityDamping;
            _rigidbody.linearVelocity += directionToGrabber / Time.fixedDeltaTime * _physicsConstants.VelocityScale;
        }

        private void TrackRotation(Quaternion targetRotation)
        {
            var rotationDelta = targetRotation * Quaternion.Inverse(_transform.rotation);
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

            _StateModule.OnGrabConfirmed -= HandleGrabConfirmed;
            _StateModule.OnDropConfirmed -= HandleDropConfirmed;
        }
    }
}
