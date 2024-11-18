using UnityEngine;
using UnityEngine.UI;
using VE2.Common;
using VE2.Core.Common;
using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.Core.Player
{

    public class Interactor2D : PointerInteractor
    {
        [SerializeField] private Image reticuleImage;

        protected override InteractorType InteractorType => InteractorType.Mouse2D;

        protected override void SubscribeToInputHandler(IInputHandler inputHandler) 
        {
            inputHandler.OnMouseLeftClick += HandleLeftClick;
            inputHandler.OnKeyboardActionKeyPressed += HandleActionKeyPressed;
            inputHandler.OnMouseScrollUp += HandleScrollUp;
            inputHandler.OnMouseScrollDown += HandleScrollDown;
        }

        private void HandleLeftClick()
        {
            if (_CurrentGrabbingGrabbable != null)
            {
                IRangedGrabInteractionModule rangedGrabInteractableToDrop = _CurrentGrabbingGrabbable;
                _CurrentGrabbingGrabbable = null;
                rangedGrabInteractableToDrop.RequestLocalDrop(_InteractorID);
            }
            else if (TryGetHoveringRangedInteractable(out IRangedInteractionModule hoveringInteractable))
            {
                if (!hoveringInteractable.AdminOnly)
                {
                    if (hoveringInteractable is IRangedClickInteractionModule rangedClickInteractable)
                    {
                        rangedClickInteractable.Click(_InteractorID.ClientID);
                    }
                    else if (hoveringInteractable is IRangedGrabInteractionModule rangedGrabInteractable)
                    {
                        _CurrentGrabbingGrabbable = rangedGrabInteractable;
                        rangedGrabInteractable.RequestLocalGrab(_InteractorID);
                    }
                }
                else 
                {
                    //TODO, maybe play an error sound or something
                }
            }
        }

        private void HandleActionKeyPressed() 
        {
            foreach (IHandheldInteractionModule handheldInteraction in _CurrentGrabbingGrabbable.HandheldInteractions)
            {
                if (handheldInteraction is IHandheldClickInteractionModule handheldClickInteraction)
                {
                    handheldClickInteraction.Click(_InteractorID.ClientID);
                }
            }
        }
        private void HandleScrollUp()
        {
            foreach (IHandheldInteractionModule handheldInteraction in _CurrentGrabbingGrabbable.HandheldInteractions)
            {
                if (handheldInteraction is IHandheldScrollInteractionModule handheldScrollInteraction)
                {
                    handheldScrollInteraction.ScrollUp(_InteractorID.ClientID);
                }
            }
        }
        private void HandleScrollDown()
        {
            foreach (IHandheldInteractionModule handheldInteraction in _CurrentGrabbingGrabbable.HandheldInteractions)
            {
                if (handheldInteraction is IHandheldScrollInteractionModule handheldScrollInteraction)
                {
                    handheldScrollInteraction.ScrollDown(_InteractorID.ClientID);
                }
            }
        }
        public override Transform ConfirmGrab(IRangedGrabInteractionModule rangedGrabInteractable)
        {
            _CurrentGrabbingGrabbable = rangedGrabInteractable;
            reticuleImage.enabled = false;
            return GrabberTransform;
        }

        public override void ConfirmDrop()
        {
            _CurrentGrabbingGrabbable = null;
            reticuleImage.enabled = true;
        }

        void Update()
        {
            if (TryGetHoveringRangedInteractable(out IRangedInteractionModule hoveringInteractable))
            {
                bool isAllowedToInteract = !hoveringInteractable.AdminOnly; 
                reticuleImage.color = isAllowedToInteract ? StaticColors.Instance.tangerine : Color.red;
                _RaycastHitDebug = hoveringInteractable.ToString();
            }
            else 
            {
                reticuleImage.color = StaticColors.Instance.lightBlue;
                _RaycastHitDebug = "none";
            }
        }

        protected override void UnsubscribeFromInputHandler(IInputHandler inputHandler)
        {
            inputHandler.OnMouseLeftClick -= HandleLeftClick;
            inputHandler.OnKeyboardActionKeyPressed -= HandleActionKeyPressed;
            inputHandler.OnMouseScrollUp -= HandleScrollUp;
            inputHandler.OnMouseScrollDown -= HandleScrollDown;
        }
    }

}
