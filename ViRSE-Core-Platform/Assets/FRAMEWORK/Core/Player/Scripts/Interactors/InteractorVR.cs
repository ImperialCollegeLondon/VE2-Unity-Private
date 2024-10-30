using UnityEngine;
using ViRSE.Core.Player;
using ViRSE.Core.VComponents;

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
        if (TryGetHoveringRangedInteractable(out IRangedPlayerInteractableImplementor hoveringInteractable))
        {
            if (hoveringInteractable is IRangedClickPlayerInteractableImplementor rangedClickInteractable)
                rangedClickInteractable.InvokeOnClickDown(_InteractorID);
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

