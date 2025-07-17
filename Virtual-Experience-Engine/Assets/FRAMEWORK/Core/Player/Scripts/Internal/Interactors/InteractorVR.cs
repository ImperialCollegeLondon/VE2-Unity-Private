using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;

namespace VE2.Core.Player.Internal
{
    internal class InteractorVR : PointerInteractor
    {
        private Vector3 _grabberTransformOffset;

        private readonly ICollisionDetector _collisionDetector;
        private readonly GameObject _goToDisableWhileGrabbing;
        private readonly LineRenderer _lineRenderer;
        private readonly Material _lineMaterial;

        private readonly IXRHapticsWrapper _xrHapticsWrapper;
        private ColorConfiguration _colorConfig => ColorConfiguration.Instance;
        private const float LINE_EMISSION_INTENSITY = 15;
        private IRangedAdjustableInteractionModule _rangedAdjustableInteractionModule;

        internal InteractorVR(HandInteractorContainer interactorContainer, IGrabInteractablesContainer grabInteractablesContainer, InteractorInputContainer interactorInputContainer, PlayerInteractionConfig playerInteractionConfig,
            InteractorReferences interactorReferences, InteractorType interactorType, IRaycastProvider raycastProvider, ICollisionDetectorFactory collisionDetectorFactory, ColliderType colliderType,
            ILocalClientIDWrapper localClientID, ILocalAdminIndicator localAdminIndicator, FreeGrabbableWrapper grabbableWrapper, HoveringOverScrollableIndicator hoveringOverScrollableIndicator, IXRHapticsWrapper xRHapticsWrapper) :
            base(interactorContainer, grabInteractablesContainer, interactorInputContainer, playerInteractionConfig,
                interactorReferences, interactorType, raycastProvider, localClientID, localAdminIndicator, grabbableWrapper, hoveringOverScrollableIndicator)

        {
            InteractorVRReferences interactorVRReferences = interactorReferences as InteractorVRReferences;

            _goToDisableWhileGrabbing = interactorVRReferences.NonGrabbingHandGO;
            _collisionDetector = collisionDetectorFactory.CreateCollisionDetector(interactorVRReferences.HandCollider, colliderType, playerInteractionConfig);

            _xrHapticsWrapper = xRHapticsWrapper;
            _lineRenderer = interactorVRReferences.LineRenderer;
            _lineMaterial = Application.isPlaying ? _lineRenderer.material : null; 
            _lineMaterial?.EnableKeyword("_EMISSION");
        }

        public override void HandleOnEnable()
        {
            base.HandleOnEnable();
            _collisionDetector.OnCollideStart += HandleCollideStart;
            _collisionDetector.OnCollideEnd += HandleCollideEnd;
        }

        public override void HandleOnDisable()
        {
            base.HandleOnDisable();
            _collisionDetector.OnCollideStart -= HandleCollideStart;
            _collisionDetector.OnCollideEnd -= HandleCollideEnd;
        }

        protected override void Vibrate(float amplitude, float duration)
        {
            _xrHapticsWrapper.Vibrate(amplitude, duration);
        }

        internal void RespondToAdjustableWithVibration()
        {
            Vibrate(HIGH_HAPTICS_AMPLITUDE, HIGH_HAPTICS_DURATION);
        }

        private void HandleCollideStart(ICollideInteractionModule collideInteractionModule)
        {
            if (_LocalClientIDWrapper.IsClientIDReady && !collideInteractionModule.AdminOnly && collideInteractionModule.CollideInteractionType == CollideInteractionType.Hand)
            {
                collideInteractionModule.InvokeOnCollideEnter(_InteractorID);
                _heldActivatableIDsAgainstNetworkFlags.Add(collideInteractionModule.ID, collideInteractionModule.IsNetworked);

                Vibrate(HIGH_HAPTICS_AMPLITUDE, HIGH_HAPTICS_DURATION);
            }
        }

        private void HandleCollideEnd(ICollideInteractionModule collideInteractionModule)
        {
            if (_LocalClientIDWrapper.IsClientIDReady && !collideInteractionModule.AdminOnly && collideInteractionModule.CollideInteractionType == CollideInteractionType.Hand)
            {
                collideInteractionModule.InvokeOnCollideExit(_InteractorID);
                _heldActivatableIDsAgainstNetworkFlags.Remove(collideInteractionModule.ID);

                Vibrate(HIGH_HAPTICS_AMPLITUDE, HIGH_HAPTICS_DURATION);
            }
        }

        protected override void HandleRaycastDistance(Vector3 point)
        {
            _lineRenderer.SetPosition(1, _RayOrigin.InverseTransformPoint(point));
        }

        protected override void SetInteractorState(InteractorState newState)
        {
            _goToDisableWhileGrabbing.SetActive(newState != InteractorState.Grabbing);

            if (_lineMaterial == null)
                return;

            switch (newState)
            {
                case InteractorState.Idle:
                    _lineMaterial.color = _colorConfig.PointerIdleColor;
                    _lineMaterial.SetColor("_EmissionColor", _colorConfig.PointerIdleColor * LINE_EMISSION_INTENSITY);
                    break;
                case InteractorState.InteractionAvailable:

                    _lineMaterial.color = _colorConfig.PointerHighlightColor;
                    _lineMaterial.SetColor("_EmissionColor", _colorConfig.PointerHighlightColor * LINE_EMISSION_INTENSITY);
                    break;
                case InteractorState.InteractionLocked:

                    _lineMaterial.color = Color.red;
                    _lineMaterial.SetColor("_EmissionColor", Color.red * LINE_EMISSION_INTENSITY);
                    break;
                case InteractorState.Grabbing:
                    break;
            }
        }

        protected override void HandleStartGrabbingAdjustable(IRangedAdjustableInteractionModule rangedAdjustableInteraction)
        {
            //We'll control its position in Update - it needs an offset towards the adjustable, without being affected by the parent transform's rotation
            _GrabberTransform.SetParent(_interactorParentTransform.parent);
            _grabberTransformOffset = rangedAdjustableInteraction.Transform.position - _GrabberTransform.position;

            //The interactor when grabbing an adjustable should listen to the ranged adjustable interaction module's value changes
            _rangedAdjustableInteractionModule = rangedAdjustableInteraction;
            _rangedAdjustableInteractionModule.OnValueChanged += RespondToAdjustableWithVibration;
        }

        protected override void HandleUpdateGrabbingAdjustable()
        {
            //offset the virtual grabber transform to the grabbable's position
            _GrabberTransform.SetPositionAndRotation(_interactorParentTransform.position + _grabberTransformOffset, _interactorParentTransform.rotation);
        }

        protected override void HandleStopGrabbingAdjustable()
        {
            //No longer apply offset to grabber, it can return to the parent 
            _GrabberTransform.SetParent(_interactorParentTransform);
            _GrabberTransform.localPosition = Vector3.zero;
            _GrabberTransform.localRotation = Quaternion.identity;

            // Unsubscribe from the ranged adjustable interaction module's value changes when stopping grabbing
            _rangedAdjustableInteractionModule.OnValueChanged -= RespondToAdjustableWithVibration;
        }
    }
}
