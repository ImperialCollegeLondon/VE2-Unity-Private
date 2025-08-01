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
        private const float MOUSE_ADJUSTABLE_SPEED = 0.005f;
        private ColorConfiguration _colorConfig => ColorConfiguration.Instance;
        private readonly Image _reticuleImage;
        private readonly PlayerConnectionPromptHandler _connectionPromptHandler;
        private readonly Interactor2DInputContainer _interactor2DInputContainer;
        private readonly InspectModeIndicator _inspectModeIndicator;
        private readonly Transform _grabberInspectGuideTransform;
        private Tween _inspectModeTween = null;
        private readonly FreeGrabbingIndicator _grabbingIndicator;
        private readonly AdjustableActiveIndicator _adjustableActiveIndicator;

        private IRangedFreeGrabInteractionModule _rangedFreeGrabbingGrabbable => _CurrentGrabbingGrabbable as IRangedFreeGrabInteractionModule;

        internal Interactor2D(HandInteractorContainer interactorContainer, IGrabInteractablesContainer grabInteractablesContainer, Interactor2DInputContainer interactor2DInputContainer,
            PlayerInteractionConfig interactionConfig, InteractorReferences interactorReferences, InteractorType interactorType, IRaycastProvider raycastProvider,
            ILocalClientIDWrapper localClientIDWrapper, ILocalAdminIndicator localAdminIndicator, InspectModeIndicator inspectModeIndicator, FreeGrabbingIndicator grabbingIndicator, AdjustableActiveIndicator adjustableActiveIndicator) :
            base(interactorContainer, grabInteractablesContainer, interactor2DInputContainer, interactionConfig,
                interactorReferences, interactorType, raycastProvider, localClientIDWrapper, localAdminIndicator, null, new HoveringOverScrollableIndicator())
        {
            Interactor2DReferences interactor2DReferences = interactorReferences as Interactor2DReferences;
            _reticuleImage = interactor2DReferences.ReticuleImage;
            _inspectModeIndicator = inspectModeIndicator;
            _grabbingIndicator = grabbingIndicator;
            _adjustableActiveIndicator = adjustableActiveIndicator;

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
                _GrabberTransform.Rotate(_grabberInspectGuideTransform.right, mouseInput.y, Space.World);
                _GrabberTransform.Rotate(_grabberInspectGuideTransform.up, -mouseInput.x, Space.World);
            }
        }

        protected override void HandleStartGrabbingAdjustable(IRangedAdjustableInteractionModule rangedAdjustableInteraction)
        {
            //Unlike VR, we should just apply a one-time offset on grab, and have the grabber behave like its on the end of a stick
            //I.E, it's position is affected by the rotation of its parent 
            Vector3 directionToGrabber = rangedAdjustableInteraction.AttachPointTransform.position - _GrabberTransform.position;
            _GrabberTransform.position += directionToGrabber;

            _adjustableActiveIndicator.SetActive(true);
        }

        protected override void HandleUpdateGrabbingAdjustable(IRangedAdjustableInteractionModule rangedAdjustableInteraction)
        {
            Vector2 mouseDelta = _interactor2DInputContainer.MouseInput.Value;

            Transform cam = VE2API.Player.ActiveCamera.transform;

            //TODO - Plane normal seems correct, but there's something wrong with the mouse move vector. 
            //When moving mouse right, I would expect to see the mouse move right across the screen, but it seems to go slightly diagonal (see line 163)

            //TODO inject camera transform reference

            //TODO - calculation of the plane normal should be done in the ranged adjustable interaction module, not here
            //if it needs the camera, we can pass it in as a parameter

            Vector3 pos = rangedAdjustableInteraction.AttachPointTransform.position;
            Vector3 adjustableAdjustmentAxis = rangedAdjustableInteraction.LocalAdjustmentAxis.normalized;

            // Try cross with up
            Vector3 orthoA = Vector3.Cross(adjustableAdjustmentAxis, Vector3.up);
            Debug.DrawRay(pos, orthoA, Color.blue);

            Vector3 planeNormal;
            if (orthoA.sqrMagnitude >= 1e-6f)
            {
                orthoA.Normalize();
                Vector3 orthoB = Vector3.Cross(adjustableAdjustmentAxis, orthoA).normalized;
                Debug.DrawRay(pos, orthoB, Color.red);

                // Pick the one closest to world up
                planeNormal = (Mathf.Abs(Vector3.Dot(orthoA, Vector3.up)) > Mathf.Abs(Vector3.Dot(orthoB, Vector3.up)))
                    ? orthoA : orthoB;
            }
            else
            {
                // Axis is vertical — find the direction toward the camera projected onto the plane
                Vector3 toCamera = cam.position - pos;

                // Project camera vector onto plane orthogonal to axis
                Vector3 projected = Vector3.ProjectOnPlane(toCamera, adjustableAdjustmentAxis);
                if (projected.sqrMagnitude >= 1e-6f)
                {
                    planeNormal = projected.normalized;
                }
                else
                {
                    // Camera is almost directly on the axis; fallback to arbitrary
                    planeNormal = Vector3.Cross(adjustableAdjustmentAxis, Vector3.right).normalized;
                }
            }

            Debug.DrawRay(pos, planeNormal, Color.white);

            // Project camera's right and up onto the plane to get movement axes
            Vector3 moveRight = Vector3.ProjectOnPlane(cam.right, planeNormal).normalized;
            Vector3 moveUp = Vector3.ProjectOnPlane(cam.up, planeNormal).normalized;

            // PROBLEM: I would expect this to line up with the yellow line (camera.right), the difference between the two is less when looking at the adjustable head on
            Debug.DrawRay(cam.position, cam.right * 10, Color.yellow);
            Debug.DrawRay(cam.position, moveRight * 10, Color.cyan);
            //===

            Debug.DrawRay(cam.position, moveUp * 10, Color.magenta);

            Vector3 moveVector = (moveRight * mouseDelta.x + moveUp * mouseDelta.y) * MOUSE_ADJUSTABLE_SPEED; 
            Debug.Log($"Move Vector: {moveVector} - mouse delta x: {mouseDelta.x} - mouse delta y: {mouseDelta.y} - moveRight: {moveRight} - moveUp: {moveUp}");

            _GrabberTransform.position += moveVector;
        }

        protected override void HandleStopGrabbingAdjustable()
        {
            _GrabberTransform.localPosition = Vector3.zero;
            _adjustableActiveIndicator.SetActive(false);
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

        public override void ConfirmGrab(string id)
        {
            base.ConfirmGrab(id);

            if (_CurrentGrabbingGrabbable is IRangedFreeGrabInteractionModule rangedFreeGrabbable)
                _grabbingIndicator.SetIsGrabbing(true, rangedFreeGrabbable);
        }

        public override void ConfirmDrop()
        {
            if (_CurrentGrabbingGrabbable is IRangedFreeGrabInteractionModule rangedFreeGrabbable)
                _grabbingIndicator.SetIsGrabbing(false, rangedFreeGrabbable);

            base.ConfirmDrop();
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
                _inspectModeTween = _GrabberTransform.DOMove(_grabberInspectGuideTransform.position, 0.3f).SetEase(Ease.InOutExpo);
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
                _inspectModeTween = _GrabberTransform.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.InOutExpo);
                _rangedFreeGrabbingGrabbable.SetInspectModeExit();
                _inspectModeIndicator.IsInspectModeActive = false;
                if (_CurrentGrabbingGrabbable == null)
                {
                    Debug.LogError("Tried to exit inspect mode, but no grabbable grabbed!");
                    return;
                }
                if (!_rangedFreeGrabbingGrabbable.PreserveInspectModeOrientation)
                    _GrabberTransform.localRotation = Quaternion.identity;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error when emitting OnInspectModeExit \n{e.Message}\n{e.StackTrace}");
            }
        }

        private void AdjustZoom(bool zoomIn)
        {
            Vector3 targetPosition = _GrabberTransform.localPosition;
            targetPosition.z = Mathf.Clamp(targetPosition.z + (zoomIn ? -INSPECT_ZOOM_SPEED : INSPECT_ZOOM_SPEED), INSPECT_MIN_ZOOM, INSPECT_MAX_ZOOM);
            _GrabberTransform.DOLocalMove(targetPosition, 0.1f);
        }

        protected override void Vibrate(float amplitude, float duration)
        {
            //Do Nothing
        }
    }
}
