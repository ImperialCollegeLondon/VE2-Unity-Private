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
            inputHandler.OnGrabKeyPressed += HandleGrabKeyPressed;
        }

        private void HandleLeftClick()
        {
            if (TryGetHoveringRangedInteractable(out IRangedInteractionModule hoveringInteractable))
            {
                if (!hoveringInteractable.AdminOnly)
                {
                    if (hoveringInteractable is IRangedClickInteractionModule rangedClickInteractable)
                        rangedClickInteractable.Click(_InteractorID.ClientID);
                }
                else 
                {
                    //TODO, maybe play an error sound or something
                }
            }
        }

        private void HandleGrabKeyPressed()
        {
            if(_CurrentGrabbingGrabbable == null)
            {
                if (TryGetHoveringRangedInteractable(out IRangedInteractionModule hoveringInteractable))
                {
                    if (!hoveringInteractable.AdminOnly)
                    {
                        if (hoveringInteractable is IRangedGrabInteractionModule rangedGrabInteractable)
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
            else
            {
                IRangedGrabInteractionModule rangedGrabInteractableToDrop = _CurrentGrabbingGrabbable;
                _CurrentGrabbingGrabbable = null;
                DropGrabbable(rangedGrabInteractableToDrop);               
            }
        }

        private void GrabGrabbable(IRangedGrabInteractionModule rangedGrabInteractable)
        {
            rangedGrabInteractable.LocalInteractorGrab(_InteractorID);
            Debug.Log("Interactor tried to Grab");
        }

        private void DropGrabbable(IRangedGrabInteractionModule rangedGrabInteractable)
        {
            rangedGrabInteractable.LocalInteractorDrop(_InteractorID);
        }

        void Update()
        {
            if (TryGetHoveringRangedInteractable(out IRangedInteractionModule hoveringInteractable))
            {
                bool isAllowedToInteract = !hoveringInteractable.AdminOnly; //TODO: Add admin check
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
