using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private readonly Interactor2DInputContainer _interactor2DInputContainer;
        private readonly InspectModeIndicator _inspectModeIndicator;
        private readonly Transform _grabberInspectGuideTransform;
        private Tween _inspectModeTween = null;
        private readonly FreeGrabbingIndicator _grabbingIndicator;
        private IsKeyboardActiveIndicator _isKeyboardActiveIndicator;

        List<TMP_InputField> _inputFields;
        private IRangedFreeGrabInteractionModule _rangedFreeGrabbingGrabbable => _CurrentGrabbingGrabbable as IRangedFreeGrabInteractionModule;

        internal Interactor2D(HandInteractorContainer interactorContainer, IGrabInteractablesContainer grabInteractablesContainer, Interactor2DInputContainer interactor2DInputContainer,
            PlayerInteractionConfig interactionConfig, InteractorReferences interactorReferences, InteractorType interactorType, IRaycastProvider raycastProvider,
            ILocalClientIDWrapper localClientIDWrapper, ILocalAdminIndicator localAdminIndicator, InspectModeIndicator inspectModeIndicator, FreeGrabbingIndicator grabbingIndicator, IsKeyboardActiveIndicator isKeyboardActiveIndicator) :
            base(interactorContainer, grabInteractablesContainer, interactor2DInputContainer, interactionConfig,
                interactorReferences, interactorType, raycastProvider, localClientIDWrapper, localAdminIndicator, null, new HoveringOverScrollableIndicator())
        {
            Interactor2DReferences interactor2DReferences = interactorReferences as Interactor2DReferences;
            _reticuleImage = interactor2DReferences.ReticuleImage;
            _inspectModeIndicator = inspectModeIndicator;
            _grabbingIndicator = grabbingIndicator;
            _isKeyboardActiveIndicator = isKeyboardActiveIndicator;

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
            DetectAllInputFieldsAndRegister();

            if (!_LocalClientIDWrapper.IsClientIDReady)
                _connectionPromptHandler.NotifyWaitingForConnection();
        }

        public override void HandleOnDisable()
        {
            base.HandleOnDisable();
            _interactor2DInputContainer.InspectModeInput.OnReleased -= HandleInspectModePressed;
            DeRegister();
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
        }

        protected override void HandleUpdateGrabbingAdjustable() { } //Nothing needed here

        protected override void HandleStopGrabbingAdjustable()
        {
            _GrabberTransform.localPosition = Vector3.zero;
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

        private void DetectAllInputFieldsAndRegister()
        {
            // Find all InputField components in the scene  
            TMP_InputField[] allInputs = Resources.FindObjectsOfTypeAll<TMP_InputField>();
            _inputFields = allInputs
                .Where(field => field != null && field.gameObject.scene.IsValid())
                .ToList();

            foreach (var inputField in _inputFields)
            {
                // Register to the OnSelect event of each InputField  
                inputField.onSelect.AddListener((_) => EnableKeyboardActiveIndicator(null, inputField));

                // Optionally, register to the OnDeselect event to reset the indicator  
                inputField.onEndEdit.AddListener(DisableKeyboardActiveIndicator);
            }
        }

        private void EnableKeyboardActiveIndicator(BaseEventData eventData, TMP_InputField inputField)
        {
            _isKeyboardActiveIndicator.IsKeyboardActive = true;
            inputField.ActivateInputField();
        }


        private void DisableKeyboardActiveIndicator(string message)
        {
            _isKeyboardActiveIndicator.IsKeyboardActive = false;
        }

        private void DeRegister()
        {
            if (_inputFields == null || _inputFields.Count == 0)
                return;
            Debug.Log("De-registering input fields from Interactor2D");
            foreach (var inputField in _inputFields)
            {
                // Unregister from the OnSelect event of each InputField  
                inputField.onSelect.RemoveListener((_) => EnableKeyboardActiveIndicator(null, inputField));

                // Optionally, unregister from the OnDeselect event to reset the indicator  
                inputField.onEndEdit.RemoveListener(DisableKeyboardActiveIndicator);
            }
            _inputFields.Clear();
        }
    }
}
