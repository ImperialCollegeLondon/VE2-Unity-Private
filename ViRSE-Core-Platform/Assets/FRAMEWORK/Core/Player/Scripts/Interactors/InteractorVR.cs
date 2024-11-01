using UnityEngine;
using ViRSE.Core.Player;
using ViRSE.Core.VComponents;
using ViRSE.Core.VComponents.PlayerInterfaces;

public class InteractorVR : BaseInteractor
{
    [SerializeField] private bool _isRightHand;

    protected override void SubscribeToInputHandler(IInputHandler inputHandler)
    {
        // if (_isRightHand)
        //     inputHandler.OnRightTriggerPressed += HandleTriggerPressed;
        // else
        //     inputHandler.OnLeftTriggerPressed += HandleTriggerPressed;
    }

    private void HandleTriggerPressed()
    {
        if (TryGetHoveringRangedInteractable(out IRangedPlayerInteractable hoveringInteractable))
        {
            if (hoveringInteractable is IRangedClickPlayerInteractable rangedClickInteractable)
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
}

