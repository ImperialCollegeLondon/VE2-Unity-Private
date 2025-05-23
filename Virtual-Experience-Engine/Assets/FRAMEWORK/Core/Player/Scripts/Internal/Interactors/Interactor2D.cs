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
        private ColorConfiguration _colorConfig => ColorConfiguration.Instance;
        private readonly Image _reticuleImage;
        private readonly PlayerConnectionPromptHandler _connectionPromptHandler;
        private Interactor2DInputContainer _interactor2DInputContainer;
        private InspectModeIndicator _inspectModeIndicator;
        private Transform _grabberInspectTransform;
        private Tween _inspectModeTween = null;
        private float verticalRotation = 0;

        private float _zoomStep = 1.0f;
        private float _minZoom = 2.0f; 
        private float _maxZoom = 5.0f; 
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
            _grabberInspectTransform = interactor2DReferences.GrabberInspectTransform;
            _interactor2DInputContainer = interactor2DInputContainer;

            //TODO: Don't want to do this in constructor, should happen in HandleOnEnable
            if (!localClientIDWrapper.IsClientIDReady)
                _connectionPromptHandler.NotifyWaitingForConnection();
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
                Vector2 mouseInput = _interactor2DInputContainer.MouseInput.Value * 0.1f;
                //float mouseX = Mouse.current.delta.x.ReadValue() * 0.1f;
                //float mouseY = Mouse.current.delta.y.ReadValue() * 0.1f;
                GrabberTransform.Rotate(mouseInput.y,-mouseInput.x,0f); //TODO - take into account camera rotation
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
                _inspectModeTween = GrabberTransform.DOMove(_grabberInspectTransform.position, 0.3f).SetEase(Ease.InOutExpo);
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
            targetPosition.z = Mathf.Clamp(targetPosition.z + (zoomIn ? -_zoomStep : _zoomStep), _minZoom, _maxZoom);
            GrabberTransform.DOLocalMove(targetPosition, 0.1f);
        }
    }
}
