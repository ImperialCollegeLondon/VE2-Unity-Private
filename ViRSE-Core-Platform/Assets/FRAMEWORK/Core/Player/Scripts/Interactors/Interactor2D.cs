using UnityEngine;
using UnityEngine.UI;
using VIRSE.Core.VComponents.InteractableInterfaces;

namespace ViRSE.Core.Player
{

    public class Interactor2D : PointerInteractor
    {
        [SerializeField] private Image reticuleImage;

        protected override void SubscribeToInputHandler(IInputHandler inputHandler) 
        {
            inputHandler.OnMouseLeftClick += HandleLeftClick;
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
