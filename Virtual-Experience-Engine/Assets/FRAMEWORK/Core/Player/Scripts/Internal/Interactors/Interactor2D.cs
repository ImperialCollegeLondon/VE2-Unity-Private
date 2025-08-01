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
        private const float MOUSE_ADJUSTABLE_SPEED = 0.008f;
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

            Camera cam = VE2API.Player.ActiveCamera;

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
                // Axis is vertical — find the direction toward the camera projected onto the plane perpendicular to the axis
                Vector3 toCamera = cam.transform.position - pos;

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

            //=== Now, we need to work out how to move the grabber transform based on the mouse delta and the normal 
            //If we move right, we want to move the grabber right across the screen, so it's more inolved than just projecting the mouse delta onto the plane
            //if the position we want to move to doesn't resolve to a position on the plane, that's fine, just don't do anything 
            //That only happens when we're looking at the adjustable from a very weird angle anyway
        
            // 1. Compute screen-space delta as before
            Vector3 screenDelta = new Vector3(mouseDelta.x, mouseDelta.y, 0);

            // 2. Compute a target screen position relative to current position
            Vector3 screenPos = cam.WorldToScreenPoint(_GrabberTransform.position);
            Vector3 targetScreenPos = screenPos + screenDelta;

            // 3. Cast ray from target screen position
            Ray targetRay = cam.ScreenPointToRay(targetScreenPos);

            // 4. Define the movement plane using the normal and the grab point
            Plane movementPlane = new Plane(planeNormal, _GrabberTransform.position);

            // 5. Intersect the ray with the movement plane
            if (movementPlane.Raycast(targetRay, out float enter))
            {
                Vector3 targetWorldPos = targetRay.GetPoint(enter);
                Vector3 rawDelta = targetWorldPos - _GrabberTransform.position;

                // Only move if the ray intersection is meaningful
                if (rawDelta.sqrMagnitude > 1e-6f)
                {
                    // Normalize and scale to get fixed world-space movement
                    Vector3 moveDirection = rawDelta.normalized;

                    _GrabberTransform.position += moveDirection * MOUSE_ADJUSTABLE_SPEED;
                }
            }
            
            Debug.DrawRay(pos, planeNormal, Color.white);
            Debug.DrawRay(cam.transform.position, cam.transform.right * 10, Color.yellow);
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
