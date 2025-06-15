using System;
using UnityEngine;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using VE2.Common.API;
using VE2.Common.Shared;
using static VE2.Common.Shared.CommonSerializables;
using VE2.Core.VComponents.Shared;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class FreeGrabbableConfig
    {
        [SerializeField, IgnoreParent] public GrabbableStateConfig StateConfig = new();

        [SerializeField, IndentArea(-1)] public RangedFreeGrabInteractionConfig RangedFreeGrabInteractionConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();

        [HideIf(nameof(MultiplayerSupportPresent), false)]
        [SerializeField, IgnoreParent] public WorldStateSyncConfig SyncConfig = new();

        private bool MultiplayerSupportPresent => VE2API.HasMultiPlayerSupport;

        public FreeGrabbableConfig(ITransformWrapper attachPointWrapper) { RangedFreeGrabInteractionConfig.AttachPointWrapper = attachPointWrapper; }
        public FreeGrabbableConfig() {}
    }

    internal class FreeGrabbableService
    {
        public IGrabbableStateModule StateModule => _StateModule;
        public IRangedFreeGrabInteractionModule RangedGrabInteractionModule => _RangedGrabInteractionModule;

        private readonly GrabbableStateModule _StateModule;
        private readonly RangedFreeGrabInteractionModule _RangedGrabInteractionModule;

        public event Action<ushort> OnGrabConfirmed;
        public event Action<ushort> OnDropConfirmed;

        private readonly IRigidbodyWrapper _rigidbody;
        private readonly PhysicsConstants _physicsConstants;
        private ITransformWrapper _transform => _config.RangedFreeGrabInteractionConfig.AttachPointWrapper;
        private IGrabbableRigidbody _grabbableRigidbodyInterface;

        private Vector3 _positionOnGrab = new();
        private Quaternion _rotationOnGrab = new();
        private Quaternion _initialGrabberToObjectRotation = Quaternion.identity;
        private bool _isKinematicOnGrab;

        private readonly FreeGrabbableConfig _config;

        public FreeGrabbableService(List<IHandheldInteractionModule> handheldInteractions, FreeGrabbableConfig config, VE2Serializable state, string id,
            IWorldStateSyncableContainer worldStateSyncableContainer, IGrabInteractablesContainer grabInteractablesContainer, HandInteractorContainer interactorContainer,
            IRigidbodyWrapper rigidbody, PhysicsConstants physicsConstants, IGrabbableRigidbody grabbableRigidbodyInterface, IClientIDWrapper localClientIdWrapper)
        {
            _config = config;

            _RangedGrabInteractionModule = new(id, grabInteractablesContainer, handheldInteractions, config.RangedFreeGrabInteractionConfig, config.GeneralInteractionConfig);
            _StateModule = new(state, config.StateConfig, config.SyncConfig, id, worldStateSyncableContainer, interactorContainer, localClientIdWrapper);

            _rigidbody = rigidbody;
            _physicsConstants = physicsConstants;
            _isKinematicOnGrab = _rigidbody.isKinematic;
            _grabbableRigidbodyInterface = grabbableRigidbodyInterface;

            _RangedGrabInteractionModule.OnLocalInteractorRequestGrab += interactorID => _StateModule.SetGrabbed(interactorID);
            _RangedGrabInteractionModule.OnLocalInteractorRequestDrop += interactorID => _StateModule.SetDropped(interactorID);
            _RangedGrabInteractionModule.OnGrabDeltaApplied += ApplyDeltaWhenGrabbed;

            _StateModule.OnGrabConfirmed += HandleGrabConfirmed;
            _StateModule.OnDropConfirmed += HandleDropConfirmed;
        }

        //This is for teleporting the grabbed object along with the player - TODO: Tweak names for clarity 
        private void ApplyDeltaWhenGrabbed(Vector3 deltaPosition, Quaternion deltaRotation)
        {
            _rigidbody.isKinematic = true;

            _rigidbody.position += deltaPosition;
            _rigidbody.rotation *= deltaRotation;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.isKinematic = false;
        }

        private void HandleGrabConfirmed(ushort grabberClientID)
        {
            if (_grabbableRigidbodyInterface.FreeGrabbableHandlesKinematics)
            {
                _isKinematicOnGrab = _rigidbody.isKinematic;
                _rigidbody.isKinematic = false;
            }
            _positionOnGrab = _rigidbody.position;
            _rotationOnGrab = _rigidbody.rotation;

            Quaternion grabberRotation = _StateModule.CurrentGrabbingInteractor.GrabberTransformWrapper.rotation;
            _initialGrabberToObjectRotation = Quaternion.Inverse(grabberRotation) * _rigidbody.rotation;

            OnGrabConfirmed?.Invoke(grabberClientID);
        }

        private void HandleDropConfirmed(ushort dropperClientID)
        {
            switch (_RangedGrabInteractionModule.DropBehaviour)
            {
                case DropBehaviour.IgnoreMomentum:
                    _rigidbody.linearVelocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                    break;
                case DropBehaviour.ReturnToPositionBeforeGrab:
                    _rigidbody.position = _positionOnGrab;
                    _rigidbody.rotation = _rotationOnGrab;
                    _rigidbody.linearVelocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                    break;
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
                TrackPosition(_StateModule.CurrentGrabbingInteractor.GrabberTransformWrapper.position);
                TrackRotation(_StateModule.CurrentGrabbingInteractor.GrabberTransformWrapper.rotation);
            }
        }

        private void TrackPosition(Vector3 targetPosition)
        {
            Vector3 directionToGrabber = targetPosition - _rigidbody.position;
            float magnitudeRatio = directionToGrabber.magnitude / _physicsConstants.DefaultMaxVelocity;

            if (magnitudeRatio > 1)
                directionToGrabber /= magnitudeRatio;

            _rigidbody.linearVelocity *= _physicsConstants.VelocityDamping;
            _rigidbody.linearVelocity += (directionToGrabber / Time.fixedDeltaTime) * _physicsConstants.VelocityScale;
        }

        private void TrackRotation(Quaternion grabberRotation)
        {
            Quaternion targetRotation = RangedGrabInteractionModule.AlignOrientationOnGrab ?
                grabberRotation :
                grabberRotation * _initialGrabberToObjectRotation;

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
