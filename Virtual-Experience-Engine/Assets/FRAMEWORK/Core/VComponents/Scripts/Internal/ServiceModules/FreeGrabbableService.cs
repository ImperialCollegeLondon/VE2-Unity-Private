using System;
using UnityEngine;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using VE2.Common.API;
using VE2.Common.Shared;
using static VE2.Common.Shared.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class FreeGrabbableConfig
    {
        [SerializeField, IgnoreParent] public GrabbableStateConfig StateConfig = new();
        [SerializeField, IgnoreParent] public FreeGrabbableInteractionConfig InteractionConfig = new();
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
        private bool _isKinematicOnGrab;
        private PhysicsConstants _physicsConstants;
        private IGrabbableRigidbody _grabbableRigidbodyInterface;

        public event Action<ushort> OnGrabConfirmed;
        public event Action<ushort> OnDropConfirmed;

        //TODO, state config shouldn't live in the service. DropBehaviour should be in ServiceConfig? or maybe the interaction module can handle drop stuff?
        private FreeGrabbableInteractionConfig _interactionConfig;

        private Vector3 positionOnGrab = new();

        public FreeGrabbableService(List<IHandheldInteractionModule> handheldInteractions, FreeGrabbableConfig config, VE2Serializable state, string id,
            IWorldStateSyncableContainer worldStateSyncableContainer, IGrabInteractablesContainer grabInteractablesContainer, HandInteractorContainer interactorContainer,
            IRigidbodyWrapper rigidbody, PhysicsConstants physicsConstants, IGrabbableRigidbody grabbableRigidbodyInterface, IClientIDWrapper localClientIdWrapper)
        {
            //even though this is never null in theory, done so to satisfy the tests
            _transform = config.InteractionConfig.AttachPoint != null ? new TransformWrapper(config.InteractionConfig.AttachPoint) : _rigidbody != null ? _rigidbody.transform : null;
            _RangedGrabInteractionModule = new(id, grabInteractablesContainer, _transform, handheldInteractions, config.InteractionConfig, config.RangedInteractionConfig, config.GeneralInteractionConfig);
            _StateModule = new(state, config.StateConfig, id, worldStateSyncableContainer, interactorContainer, localClientIdWrapper);
            _interactionConfig = config.InteractionConfig;

            _rigidbody = rigidbody;
            _physicsConstants = physicsConstants;
            _isKinematicOnGrab = _rigidbody.isKinematic;
            _grabbableRigidbodyInterface = grabbableRigidbodyInterface;

            _RangedGrabInteractionModule.OnLocalInteractorRequestGrab += (InteractorID interactorID) => _StateModule.SetGrabbed(interactorID);
            _RangedGrabInteractionModule.OnLocalInteractorRequestDrop += (InteractorID interactorID) => _StateModule.SetDropped(interactorID);
            _RangedGrabInteractionModule.OnGrabDeltaApplied += ApplyDeltaWhenGrabbed;

            _StateModule.OnGrabConfirmed += HandleGrabConfirmed;
            _StateModule.OnDropConfirmed += HandleDropConfirmed;
        }

        //This is for teleporting the grabbed object along with the player - TODO: Tweak names for clarity 
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

        // private void HandleLocalInteractorRequestGrab(InteractorID interactorID) =>  _StateModule.SetGrabbed(interactorID);

        // private void HandleLocalInteractorRequestDrop(InteractorID interactorID) => _StateModule.SetDropped(interactorID);

        private void HandleGrabConfirmed(ushort grabberClientID)
        {
            if (_grabbableRigidbodyInterface.FreeGrabbableHandlesKinematics)
            {
                _isKinematicOnGrab = _rigidbody.isKinematic;
                _rigidbody.isKinematic = false;
            }
            positionOnGrab = _rigidbody.position;
            OnGrabConfirmed?.Invoke(grabberClientID);
        }

        private void HandleDropConfirmed(ushort dropperClientID)
        {
            // Handle drop behaviours
            if (_interactionConfig.dropBehaviour == DropBehaviour.IgnoreMomentum)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            else if (_interactionConfig.dropBehaviour == DropBehaviour.ReturnToPositionBeforeGrab)
            {
                _rigidbody.position = positionOnGrab;
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            OnDropConfirmed?.Invoke(dropperClientID);

            if (_grabbableRigidbodyInterface.FreeGrabbableHandlesKinematics)
            {
                _rigidbody.isKinematic = _isKinematicOnGrab;
            }
        }

        public void HandleFixedUpdate()
        {
            _StateModule.HandleFixedUpdate();
            if (_StateModule.IsGrabbed)
            {
                TrackPosition(_StateModule.CurrentGrabbingInteractor.GrabberTransform.position);
                TrackRotation(_StateModule.CurrentGrabbingInteractor.GrabberTransform.rotation);
            }
        }

        private void TrackPosition(Vector3 targetPosition)
        {
            Vector3 directionToGrabber = targetPosition - _rigidbody.position;
            float directionToGrabberMaxVelocityMagnitudeRatio = directionToGrabber.magnitude / _physicsConstants.DefaultMaxVelocity;

            if (directionToGrabberMaxVelocityMagnitudeRatio > 1)
                directionToGrabber /= directionToGrabberMaxVelocityMagnitudeRatio;

            _rigidbody.linearVelocity *= _physicsConstants.VelocityDamping;
            _rigidbody.linearVelocity += (directionToGrabber / Time.fixedDeltaTime) * _physicsConstants.VelocityScale;
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
            _RangedGrabInteractionModule.TearDown();

            _StateModule.OnGrabConfirmed -= HandleGrabConfirmed;
            _StateModule.OnDropConfirmed -= HandleDropConfirmed;
        }
    }
}
