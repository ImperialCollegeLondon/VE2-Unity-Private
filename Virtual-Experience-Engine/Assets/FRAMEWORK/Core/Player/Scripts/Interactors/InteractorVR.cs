using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using VE2.Core.Player;
using VE2.Core.VComponents.InteractableInterfaces;

public class InteractorVR : PointerInteractor
{
    [SerializeField] private bool _isRightHand;

    protected override InteractorType InteractorType => _isRightHand ? InteractorType.RightHandVR : InteractorType.LeftHandVR;

    protected override void SubscribeToInputHandler(IInputHandler inputHandler)
    {
        // if (_isRightHand)
        //     inputHandler.OnRightTriggerPressed += HandleTriggerPressed;
        // else
        //     inputHandler.OnLeftTriggerPressed += HandleTriggerPressed;
    }

    private void HandleTriggerPressed()
    {
        if (TryGetHoveringRangedInteractable(out IRangedInteractionModule hoveringInteractable))
        {
            if (hoveringInteractable is IRangedClickInteractionModule rangedClickInteractable)
                rangedClickInteractable.Click(_InteractorID.ClientID);
        }
    }

    protected override void UnsubscribeFromInputHandler(IInputHandler inputHandler)
    {
        // if (_isRightHand)
        //     inputHandler.OnRightTriggerPressed -= HandleTriggerPressed;
        // else
        //     inputHandler.OnLeftTriggerPressed -= HandleTriggerPressed;
    }

    public override Transform ConfirmGrab()
    {
        //TODO:
        return null;
    }

    public override void ConfirmDrop()
    {
        //TODO:
    }
}

