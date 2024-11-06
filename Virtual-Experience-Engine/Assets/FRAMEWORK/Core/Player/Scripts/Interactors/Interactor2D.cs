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
        }

        private void HandleLeftClick()
        {
            if (_CurrentGrabbingGrabbable != null)
            {
                IRangedGrabInteractionModule rangedGrabInteractableToDrop = _CurrentGrabbingGrabbable;
                _CurrentGrabbingGrabbable = null;
                DropGrabbable(rangedGrabInteractableToDrop);
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
                        GrabGrabbable(rangedGrabInteractable);
                    }
                }
                else 
                {
                    //TODO, maybe play an error sound or something
                }
            }
        }

        private void GrabGrabbable(IRangedGrabInteractionModule rangedGrabInteractable)
        {
            reticuleImage.enabled = false;
            rangedGrabInteractable.LocalInteractorGrab(_InteractorID);
            Debug.Log("Interactor tried to Grab");
        }

        private void DropGrabbable(IRangedGrabInteractionModule rangedGrabInteractable)
        {
            reticuleImage.enabled = true;
            rangedGrabInteractable.LocalInteractorDrop(_InteractorID);
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
        }
    }

}
