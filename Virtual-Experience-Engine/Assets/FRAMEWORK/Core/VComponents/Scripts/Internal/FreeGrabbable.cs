using System;
using UnityEngine;
using VE2.Common;
using VE2.Core.VComponents.NonInteractableInterfaces;
using VE2.Core.VComponents.InteractableInterfaces;
using static VE2.Common.CommonSerializables;
using log4net.Util;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    public class FreeGrabbableConfig
    {
        [SerializeField, IgnoreParent] public FreeGrabbableStateConfig StateConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
        [SerializeField, IgnoreParent] public RangedInteractionConfig RangedInteractionConfig = new();
    }

    public class FreeGrabbable
    {
        #region Interfacess
        public IFreeGrabbableStateModule StateModule => _StateModule;
        public IRangedGrabInteractionModule RangedGrabInteractionModule => _RangedGrabInteractionModule;
        #endregion

        #region Modules
        private readonly FreeGrabbableStateModule _StateModule;
        private readonly RangedGrabInteractionModule _RangedGrabInteractionModule;
        #endregion

        private Rigidbody _rigidbody;
        private const float DEFAULT_MAX_VELOCITY = 10;
        private const float DEFAULT_MAX_ANGULAR_VELOCITY = 10;
        private const float VELOCITY_SCALE = 0.35f;
        private const float VELOCITY_DAMPING = 0.45f;
        private const float ANGULAR_VELOCITY_SCALE = 0.35f;
        private const float ANGULAR_VELOCITY_DAMPING = 0.45f;

        public FreeGrabbable(FreeGrabbableConfig config, VE2Serializable state, string id, WorldStateModulesContainer worldStateModulesContainer, IGameObjectFindProvider gameObjectFindProvider, Rigidbody rigidbody)
        {
            _StateModule = new(state, config.StateConfig, id, worldStateModulesContainer, gameObjectFindProvider);
            _rigidbody  = rigidbody;
            _RangedGrabInteractionModule = new(config.RangedInteractionConfig, config.GeneralInteractionConfig);

            _RangedGrabInteractionModule.OnLocalInteractorGrab += HandleGrabFromLocalInteractor;
            _RangedGrabInteractionModule.OnLocalInteractorDrop += HandleDropFromLocalInteractor;
        }

        private void HandleGrabFromLocalInteractor(InteractorID interactorID)
        {
            _StateModule.SetGrabbed(interactorID);
        }

        private void HandleDropFromLocalInteractor(InteractorID interactorID)
        {
            _StateModule.SetDropped(interactorID);
        }

        public void HandleFixedUpdate()
        {
            _StateModule.HandleFixedUpdate();
            if(_StateModule.IsGrabbed)
            {
                Debug.Log("State Module is Grabbed");
                TrackPosition(_StateModule.CurrentGrabbingGrabberTransform.position);
                TrackRotation(_StateModule.CurrentGrabbingGrabberTransform.rotation);
            }
        }

        private void TrackPosition(Vector3 targetPosition)
        {
            Vector3 directionToGrabber = targetPosition - _rigidbody.position;
            float directionToGrabberMaxVelocityMagnitudeRatio = directionToGrabber.magnitude / DEFAULT_MAX_VELOCITY;
            if (directionToGrabberMaxVelocityMagnitudeRatio > 1)
                directionToGrabber /= directionToGrabberMaxVelocityMagnitudeRatio;
            _rigidbody.linearVelocity *= VELOCITY_DAMPING;
            _rigidbody.linearVelocity += (directionToGrabber / Time.fixedDeltaTime * VELOCITY_SCALE);
        }

        private void TrackRotation(Quaternion targetRotation)
        {
            var rotationDelta = targetRotation * Quaternion.Inverse(_rigidbody.rotation);
            rotationDelta.ToAngleAxis(out var angleInDegrees, out var rotationAxis);
            if (angleInDegrees > 180f)
                angleInDegrees -= 360f;
            var angularVelocity = (rotationAxis * (angleInDegrees * Mathf.Deg2Rad));
            _rigidbody.angularVelocity *= ANGULAR_VELOCITY_DAMPING;
            _rigidbody.angularVelocity += (angularVelocity / Time.fixedDeltaTime * ANGULAR_VELOCITY_SCALE);
        }

        public void TearDown()
        {
            _StateModule.TearDown();
        }
    }
}
