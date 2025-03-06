using UnityEngine;
using VE2.Core.Common;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;

namespace VE2.Core.Player.Internal
{
    internal class InteractorVR : PointerInteractor
    {
        private readonly V_CollisionDetector _collisionDetector;
        private readonly GameObject _handVisualGO;
        private readonly LineRenderer _lineRenderer;
        private readonly Material _lineMaterial;
        private readonly ColorConfiguration _colorConfig;
        private const float LINE_EMISSION_INTENSITY = 15;

        internal InteractorVR(InteractorContainer interactorContainer, InteractorInputContainer interactorInputContainer,
            InteractorReferences interactorReferences, InteractorType interactorType, IRaycastProvider raycastProvider, ILocalClientIDProvider multiplayerSupport, FreeGrabbableWrapper grabbableWrapper) :
            base(interactorContainer, interactorInputContainer,
                interactorReferences, interactorType, raycastProvider, multiplayerSupport, grabbableWrapper)
        {
            InteractorVRReferences interactorVRReferences = interactorReferences as InteractorVRReferences;

            _collisionDetector = interactorVRReferences.CollisionDetector;
            _handVisualGO = interactorVRReferences.HandVisualGO;

            _lineRenderer = interactorVRReferences.LineRenderer;
            _lineMaterial = _lineRenderer.material;
            _lineMaterial.EnableKeyword("_EMISSION");

            _colorConfig = Resources.Load<ColorConfiguration>("ColorConfiguration"); //TODO: Inject
        }

        public override void HandleOnEnable()
        {
            base.HandleOnEnable();
            _collisionDetector.OnCollideStart += HandleCollideStart;
            _collisionDetector.OnCollideEnd += HandleCollideEnd;
        }

        public override void HandleOnDisable()
        {
            base.HandleOnEnable();
            _collisionDetector.OnCollideStart += HandleCollideStart;
            _collisionDetector.OnCollideEnd += HandleCollideEnd;
        }

        private void HandleCollideStart(ICollideInteractionModule collideInteractionModule)
        {
            if (!_WaitingForLocalClientID && !collideInteractionModule.AdminOnly)
                collideInteractionModule.InvokeOnCollideEnter(_InteractorID.ClientID);
        }

        private void HandleCollideEnd(ICollideInteractionModule collideInteractionModule)
        {
            if (!_WaitingForLocalClientID && !collideInteractionModule.AdminOnly)
                collideInteractionModule.InvokeOnCollideExit(_InteractorID.ClientID);
        }

        protected override void HandleRaycastDistance(float distance)
        {
            _lineRenderer.SetPosition(1, new Vector3(0, 0, distance / _lineRenderer.transform.lossyScale.z));
        }

        protected override void SetInteractorState(InteractorState newState)
        {
            _handVisualGO.SetActive(newState != InteractorState.Grabbing);

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
    }
}
