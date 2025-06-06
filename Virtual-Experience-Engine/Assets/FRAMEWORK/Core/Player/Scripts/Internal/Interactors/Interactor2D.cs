using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;

namespace VE2.Core.Player.Internal
{
    internal class Interactor2D : PointerInteractor
    {
        private const float INSPECT_ZOOM_SPEED = 1.0f;
        private const float INSPECT_MIN_ZOOM = 2.0f;
        private const float INSPECT_MAX_ZOOM = 5.0f;
        private const float INSPECT_ROTATE_SPEED = 0.1f;

        private ColorConfiguration _colorConfig => ColorConfiguration.Instance;
        private readonly Image _reticuleImage;
        private readonly PlayerConnectionPromptHandler _connectionPromptHandler;
        private Interactor2DInputContainer _interactor2DInputContainer;
        private InspectModeIndicator _inspectModeIndicator;
        private Transform _grabberInspectGuideTransform;
        private Tween _inspectModeTween = null;

        private IRangedFreeGrabInteractionModule _rangedFreeGrabbingGrabbable => _CurrentGrabbingGrabbable as IRangedFreeGrabInteractionModule;

        internal Interactor2D(HandInteractorContainer interactorContainer, IGrabInteractablesContainer grabInteractablesContainer, Interactor2DInputContainer interactor2DInputContainer,
            PlayerInteractionConfig interactionConfig, InteractorReferences interactorReferences, InteractorType interactorType, IRaycastProvider raycastProvider,
            ILocalClientIDWrapper localClientIDWrapper, InspectModeIndicator inspectModeIndicator) :
            base(interactorContainer, grabInteractablesContainer, interactor2DInputContainer, interactionConfig,
                interactorReferences, interactorType, raycastProvider, localClientIDWrapper, null, new HoveringOverScrollableIndicator())
        {
            Interactor2DReferences interactor2DReferences = interactorReferences as Interactor2DReferences;
            _reticuleImage = interactor2DReferences.ReticuleImage;
            _inspectModeIndicator = inspectModeIndicator;

            _connectionPromptHandler = interactor2DReferences.ConnectionPromptHandler;
            _grabberInspectGuideTransform = interactor2DReferences.GrabberInspectTransform;
            _interactor2DInputContainer = interactor2DInputContainer;
        }

        protected override void SetInteractorState(InteractorState newState)
        {
            _reticuleImage.enabled = newState != InteractorState.Grabbing;

            switch (newState)
            {
                case InteractorState.Idle:
                    _reticuleImage.color = _colorConfig.PointerIdleColor;
                    break;
                case InteractorState.InteractionAvailable:
                    _reticuleImage.color = _colorConfig.PointerHighlightColor;
                    break;
                case InteractorState.InteractionLocked:
                    _reticuleImage.color = Color.red;
                    break;
                case InteractorState.Grabbing:
                    //No colour 
                    break;
            }
        }

        public override void HandleOnEnable()
        {
            base.HandleOnEnable();
            _interactor2DInputContainer.InspectModeInput.OnReleased += HandleInspectModePressed;

            if (!_LocalClientIDWrapper.IsClientIDReady)
                _connectionPromptHandler.NotifyWaitingForConnection();
        }

        public override void HandleOnDisable()
        {
            base.HandleOnDisable();
            _interactor2DInputContainer.InspectModeInput.OnReleased -= HandleInspectModePressed;
        }

        public override void HandleUpdate()
        {
            base.HandleUpdate();

            if (_inspectModeIndicator.IsInspectModeActive)
            {
                Vector2 mouseInput = _interactor2DInputContainer.MouseInput.Value * INSPECT_ROTATE_SPEED;

                //Rotate relative to the inspect guide transform's local axes
                GrabberTransform.Rotate(_grabberInspectGuideTransform.right, mouseInput.y, Space.World);
                GrabberTransform.Rotate(_grabberInspectGuideTransform.up, -mouseInput.x, Space.World);
            }
        }

        protected override void HandleStartGrabbingAdjustable(IRangedAdjustableInteractionModule rangedAdjustableInteraction)
        {
            //Unlike VR, we should just apply a one-time offset on grab, and have the grabber behave like its on the end of a stick
            //I.E, it's position is affected by the rotation of its parent 
            Vector3 directionToGrabber = rangedAdjustableInteraction.Transform.position - GrabberTransform.position;
            GrabberTransform.position += directionToGrabber;
        }

        protected override void HandleUpdateGrabbingAdjustable() { } //Nothing needed here

        protected override void HandleStopGrabbingAdjustable()
        {
            GrabberTransform.localPosition = Vector3.zero;
        }

        protected override void HandleLocalClientIDReady(ushort clientID)
        {
            base.HandleLocalClientIDReady(clientID);

            _connectionPromptHandler.NotifyConnected();
        }

        protected override void CheckForExitInspectMode()
        {
            if (_inspectModeIndicator.IsInspectModeActive)
                ExitInspectMode();
        }

        protected override void CheckForInspectModeOnScroll(bool scrollUp)
        {
            if (_inspectModeIndicator.IsInspectModeActive)
            {
                if (scrollUp)
                    AdjustZoom(false);
                else
                    AdjustZoom(true);
                return;
            }
        }

        private void HandleInspectModePressed()
        {
            if (!IsCurrentlyGrabbing)
            {
                return;
            }

            if (_inspectModeTween != null && _inspectModeTween.IsPlaying())
                _inspectModeTween.Kill();

            if (!_inspectModeIndicator.IsInspectModeActive)
                EnterInspectMode();
            else
                ExitInspectMode();
        }

        private void EnterInspectMode()
        {
            try
            {
                _inspectModeTween = GrabberTransform.DOMove(_grabberInspectGuideTransform.position, 0.3f).SetEase(Ease.InOutExpo);
                _rangedFreeGrabbingGrabbable.NotifyInspectModeEnter();
                _inspectModeIndicator.IsInspectModeActive = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error when emitting OnInspectModeEnter \n{e.Message}\n{e.StackTrace}");
            }
        }

        private void ExitInspectMode()
        {
            try
            {
                _inspectModeTween = GrabberTransform.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.InOutExpo);
                _rangedFreeGrabbingGrabbable.SetInspectModeExit();
                _inspectModeIndicator.IsInspectModeActive = false;
                if (_CurrentGrabbingGrabbable == null)
                {
                    Debug.LogError("Tried to exit inspect mode, but no grabbable grabbed!");
                    return;
                }
                if (!_rangedFreeGrabbingGrabbable.PreserveInspectModeOrientation)
                    GrabberTransform.localRotation = Quaternion.identity;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error when emitting OnInspectModeExit \n{e.Message}\n{e.StackTrace}");
            }
        }

        private void AdjustZoom(bool zoomIn)
        {
            Vector3 targetPosition = GrabberTransform.localPosition;
            targetPosition.z = Mathf.Clamp(targetPosition.z + (zoomIn ? -INSPECT_ZOOM_SPEED : INSPECT_ZOOM_SPEED), INSPECT_MIN_ZOOM, INSPECT_MAX_ZOOM);
            GrabberTransform.DOLocalMove(targetPosition, 0.1f);
        }

        protected override void Vibrate(float amplitude, float duration)
        {
            //Do Nothing
        }
    }
}
