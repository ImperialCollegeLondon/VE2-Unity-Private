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
        private readonly GameObject _handVisualGO;
        private readonly LineRenderer _lineRenderer;
        private readonly Material _lineMaterial;
        private ColorConfiguration _colorConfig => ColorConfiguration.Instance;
        private const float LINE_EMISSION_INTENSITY = 15;

        internal InteractorVR(HandInteractorContainer interactorContainer, IGrabInteractablesContainer grabInteractablesContainer, InteractorInputContainer interactorInputContainer, PlayerInteractionConfig playerInteractionConfig,
            InteractorReferences interactorReferences, InteractorType interactorType, IRaycastProvider raycastProvider, ICollisionDetectorFactory collisionDetectorFactory, ColliderType colliderType,
            ILocalClientIDWrapper localClientID, FreeGrabbableWrapper grabbableWrapper, HoveringOverScrollableIndicator hoveringOverScrollableIndicator) :
            base(interactorContainer, grabInteractablesContainer, interactorInputContainer, playerInteractionConfig,
                interactorReferences, interactorType, raycastProvider, localClientID, grabbableWrapper, hoveringOverScrollableIndicator)

        {
            InteractorVRReferences interactorVRReferences = interactorReferences as InteractorVRReferences;

            _handVisualGO = interactorVRReferences.HandVisualGO;
            _collisionDetector = collisionDetectorFactory.CreateCollisionDetector(interactorVRReferences.HandCollider, colliderType, playerInteractionConfig.InteractableLayers);

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

        private void HandleCollideStart(ICollideInteractionModule collideInteractionModule)
        {
            if (_LocalClientIDWrapper.IsClientIDReady && !collideInteractionModule.AdminOnly && collideInteractionModule.CollideInteractionType == CollideInteractionType.Hand)
            {
                collideInteractionModule.InvokeOnCollideEnter(_InteractorID);
                HeldActivatableIDs.Add(collideInteractionModule.ID);
            }
        }

        private void HandleCollideEnd(ICollideInteractionModule collideInteractionModule)
        {
            if (_LocalClientIDWrapper.IsClientIDReady && !collideInteractionModule.AdminOnly && collideInteractionModule.CollideInteractionType == CollideInteractionType.Hand)
            {
                collideInteractionModule.InvokeOnCollideExit(_InteractorID);
                HeldActivatableIDs.Remove(collideInteractionModule.ID);
            }
        }

        protected override void HandleRaycastDistance(float distance, bool isOnPalm = false, Vector3 point = default)
        {
            if(!isOnPalm)
                _lineRenderer.SetPosition(1, new Vector3(0, 0, distance / _lineRenderer.transform.lossyScale.z));
            else
                _lineRenderer.SetPosition(1, _RayOrigin.InverseTransformPoint(point));
        }

        protected override void SetInteractorState(InteractorState newState)
        {
            _handVisualGO.SetActive(newState != InteractorState.Grabbing);

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
            _grabberTransformOffset = rangedAdjustableInteraction.Transform.position - GrabberTransform.position;
        }

        protected override void HandleUpdateGrabbingAdjustable()
        {
            //offset the virtual grabber transform to the grabbable's position
            GrabberTransform.SetPositionAndRotation(_interactorParentTransform.position + _grabberTransformOffset, _interactorParentTransform.rotation);
        }

        protected override void HandleStopGrabbingAdjustable()
        {
            //No longer apply offset to grabber, it can return to the parent 
            _GrabberTransform.SetParent(_interactorParentTransform);
            _GrabberTransform.localPosition = Vector3.zero;
            _GrabberTransform.localRotation = Quaternion.identity;
        }

    }
}
