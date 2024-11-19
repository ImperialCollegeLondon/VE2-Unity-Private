using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using VE2.Core.Player;

public class InteractorVR : PointerInteractor
{
    protected override void SetInteractorState(InteractorState newState)
    {
        switch (newState)
        {
            case InteractorState.Idle:

                break;
            case InteractorState.InteractionAvailable:

                break;
            case InteractorState.InteractionLocked:

                break;
            case InteractorState.Grabbing:

                break;
        }
    }
}

