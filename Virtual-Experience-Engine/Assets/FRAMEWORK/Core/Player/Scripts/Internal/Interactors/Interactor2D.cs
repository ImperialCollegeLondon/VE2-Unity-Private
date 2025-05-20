using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;
using VE2.Core.Common;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;
using UnityEngine.InputSystem;

namespace VE2.Core.Player.Internal
{
    internal class Interactor2D : PointerInteractor
    {
        private readonly Image _reticuleImage;
        private readonly ColorConfiguration _colorConfig;
        private readonly PlayerConnectionPromptHandler _connectionPromptHandler;
        private Player2DInputContainer _player2DInputContainer;
        private InspectModeIndicator _inspectModeIndicator;
        private Transform _grabberInspectTransform;
        private Tween _inspectModeTween = null;
        private float verticalRotation = 0;

        private float _zoomStep = 1.0f;
        private float _minZoom = 2.0f; 
        private float _maxZoom = 5.0f; 
        private IRangedFreeGrabInteractionModule _rangedFreeGrabbingGrabbable => _CurrentGrabbingGrabbable as IRangedFreeGrabInteractionModule;

        internal Interactor2D(HandInteractorContainer interactorContainer, InteractorInputContainer interactorInputContainer,
            InteractorReferences interactorReferences, InteractorType interactorType, IRaycastProvider raycastProvider,
            ILocalClientIDProvider localClientIDProvider, Player2DInputContainer player2DInputContainer, InspectModeIndicator inspectModeIndicator) :
            base(interactorContainer, interactorInputContainer,
                interactorReferences, interactorType, raycastProvider, localClientIDProvider, null, new HoveringOverScrollableIndicator())
        {
            Interactor2DReferences interactor2DReferences = interactorReferences as Interactor2DReferences;
            _reticuleImage = interactor2DReferences.ReticuleImage;
            _inspectModeIndicator = inspectModeIndicator;

            _colorConfig = Resources.Load<ColorConfiguration>("ColorConfiguration"); //TODO: Inject, can probably actually go into the base class

            _connectionPromptHandler = interactor2DReferences.ConnectionPromptHandler;
            _grabberInspectTransform = interactor2DReferences.GrabberInspectTransform;
            _player2DInputContainer = player2DInputContainer;
            if (_WaitingForLocalClientID)
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
            _player2DInputContainer.InspectModeButton.OnReleased += HandleInspectModePressed;
        }

        public override void HandleOnDisable()
        {
            base.HandleOnDisable();
            _player2DInputContainer.InspectModeButton.OnReleased -= HandleInspectModePressed;
        }

        public override void HandleUpdate()
        {
            base.HandleUpdate();

            if (_inspectModeIndicator.IsInspectModeEnabled)
            {
                float mouseX = Mouse.current.delta.x.ReadValue() * 0.1f;


                float mouseY = Mouse.current.delta.y.ReadValue() * 0.1f;
                GrabberTransform.Rotate(mouseY,-mouseX,0f);
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

        protected override void HandleGrabPressed()
        {
            if (IsCurrentlyGrabbing)
            {
                if(_inspectModeIndicator.IsInspectModeEnabled)
                    ExitInspectMode();

                IRangedGrabInteractionModule rangedGrabInteractableToDrop = _CurrentGrabbingGrabbable;
                rangedGrabInteractableToDrop.RequestLocalDrop(_InteractorID);
            }
            else
            {
                RaycastResultWrapper raycastResultWrapper = GetRayCastResult();

                if (!_WaitingForLocalClientID && raycastResultWrapper != null && raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange)
                {
                    if (!raycastResultWrapper.RangedInteractable.AdminOnly)
                    {
                        if (raycastResultWrapper.RangedInteractable is IRangedGrabInteractionModule rangedGrabInteractable)
                        {
                            rangedGrabInteractable.RequestLocalGrab(_InteractorID);
                        }
                    }
                    else
                    {
                        //TODO, maybe play an error sound or something
                    }
                }
            }
        }

        protected override void HandleScrollUp()
        {
            if (IsCurrentlyGrabbing)
            {
                if (_inspectModeIndicator.IsInspectModeEnabled)
                {
                    Debug.Log("Zoom Feature Running Scroll Up");
                    ZoomIn();
                }
                else
                {
                    foreach (IHandheldInteractionModule handheldInteraction in _CurrentGrabbingGrabbable.HandheldInteractions)
                    {
                        if (handheldInteraction is IHandheldScrollInteractionModule handheldScrollInteraction)
                        {
                            handheldScrollInteraction.ScrollUp(_InteractorID.ClientID);
                        }
                    }
                }
            }
            else
            {
                RaycastResultWrapper raycastResultWrapper = GetRayCastResult();

                if (!_WaitingForLocalClientID && raycastResultWrapper != null && raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange)
                {
                    if (!raycastResultWrapper.RangedInteractable.AdminOnly)
                    {
                        //if while scrolling up, raycast returns an adjustable module
                        if (raycastResultWrapper.RangedInteractable is IRangedAdjustableInteractionModule rangedAdjustableInteraction)
                        {
                            rangedAdjustableInteraction.ScrollUp();
                        }
                    }
                }
            }
        }

        private void ZoomIn()
        {
            Vector3 targetPosition = GrabberTransform.localPosition;
            targetPosition.z = Mathf.Clamp(targetPosition.z - _zoomStep, _minZoom, _maxZoom);
            GrabberTransform.DOLocalMove(targetPosition, 0.1f); 
        }

        private void ZoomOut()
        {
            Vector3 targetPosition = GrabberTransform.localPosition;
            targetPosition.z = Mathf.Clamp(targetPosition.z + _zoomStep, _minZoom, _maxZoom);
            GrabberTransform.DOLocalMove(targetPosition, 0.1f);
        }

        protected override void HandleScrollDown()
        {
            if (IsCurrentlyGrabbing)
            {
                if (_inspectModeIndicator.IsInspectModeEnabled)
                {
                    Debug.Log("Zoom Feature Running Scroll Down");
                    ZoomOut();
                }
                else
                {
                    foreach (IHandheldInteractionModule handheldInteraction in _CurrentGrabbingGrabbable.HandheldInteractions)
                    {
                        if (handheldInteraction is IHandheldScrollInteractionModule handheldScrollInteraction)
                        {
                            handheldScrollInteraction.ScrollDown(_InteractorID.ClientID);
                        }
                    }
                }
            }
            else
            {
                RaycastResultWrapper raycastResultWrapper = GetRayCastResult();

                if (!_WaitingForLocalClientID && raycastResultWrapper != null && raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange)
                {
                    if (!raycastResultWrapper.RangedInteractable.AdminOnly)
                    {
                        //if while scrolling up, raycast returns an adjustable module
                        if (raycastResultWrapper.RangedInteractable is IRangedAdjustableInteractionModule rangedAdjustableInteraction)
                        {
                            rangedAdjustableInteraction.ScrollDown();
                        }
                    }
                }
            }
        }

        private void HandleInspectModePressed()
        {
            if (!IsCurrentlyGrabbing)
            {
                Debug.LogWarning("ToggleInspectMode - Cannot toggle inspect mode while no object is grabbed. Ignoring request.");
                return;
            }

            if (_inspectModeTween != null && _inspectModeTween.IsPlaying())
                _inspectModeTween.Kill();

            if (!_inspectModeIndicator.IsInspectModeEnabled)
                EnterInspectMode();
            else
                ExitInspectMode();
        }

        private void EnterInspectMode()
        {
            Debug.Log("We are now in Inspect Mode");

            try
            {
                _inspectModeTween = GrabberTransform.DOMove(_grabberInspectTransform.position, 0.3f).SetEase(Ease.InOutExpo);
                _rangedFreeGrabbingGrabbable.SetInspectModeEnter();
                _inspectModeIndicator.IsInspectModeEnabled = true;
            }
            catch (Exception e)
            {
                Debug.Log($"Error when emitting OnInspectModeEnter \n{e.Message}\n{e.StackTrace}");
            }
        }

        private void ExitInspectMode()
        {
            Debug.Log("We are now out of Inspect Mode");

            try
            {
                _inspectModeTween = GrabberTransform.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.InOutExpo);
                _rangedFreeGrabbingGrabbable.SetInspectModeExit();
                _inspectModeIndicator.IsInspectModeEnabled = false;

                if (_CurrentGrabbingGrabbable == null)
                {
                    V_Logger.Error("Tried to exit inspect mode, but no grabbable grabbed!");
                    return;
                }

                GrabberTransform.localRotation = Quaternion.identity;
            }
            catch (Exception e)
            {
                Debug.Log($"Error when emitting OnInspectModeExit \n{e.Message}\n{e.StackTrace}");
            }
        }
    }
}
