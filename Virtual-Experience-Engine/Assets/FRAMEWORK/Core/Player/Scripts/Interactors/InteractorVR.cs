using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using VE2.Core.Player;
using VE2.Core.Player.InteractionFinders;
using VE2.Core.VComponents.InteractableInterfaces;

public class InteractorVR : PointerInteractor
{
    private CollisionDetector _collisionDetector;
    private LineRenderer _lineRenderer;

    public void InitializeVR(Transform rayOrigin, InteractorType interactorType, IMultiplayerSupport multiplayerSupport, InteractorInputContainer interactorInputContainer, IRaycastProvider raycastProvider,
        CollisionDetector collisionDetector, LineRenderer lineRenderer) 
    {
        base.Initialize(rayOrigin, interactorType, multiplayerSupport, interactorInputContainer, raycastProvider);
        _collisionDetector = collisionDetector;
        _lineRenderer = lineRenderer;
    }

    public override void HandleOnEnable()
    {
        base.HandleOnEnable();
        _collisionDetector.OnCollideStart += HandleCollideStart;
        _collisionDetector.OnCollideEnd += HandleCollideEnd;
    }

    public override void HandleOnDisable()
    {
        base.HandleOnEnable();
        _collisionDetector.OnCollideStart += HandleCollideStart;
        _collisionDetector.OnCollideEnd += HandleCollideEnd;
    }

    private void HandleCollideStart(ICollideInteractionModule collideInteractionModule)
    {
        if (!_WaitingForMultiplayerSupport && !collideInteractionModule.AdminOnly)
            collideInteractionModule.InvokeOnCollideEnter(_InteractorID.ClientID);
    }

    private void HandleCollideEnd(ICollideInteractionModule collideInteractionModule)
    {
        if (!_WaitingForMultiplayerSupport && !collideInteractionModule.AdminOnly)
            collideInteractionModule.InvokeOnCollideExit(_InteractorID.ClientID);
    }

    protected override void SetInteractorState(InteractorState newState)
    {
        _lineRenderer.enabled = newState != InteractorState.Grabbing;

        switch (newState)
        {
            case InteractorState.Idle:
                _lineRenderer.startColor = StaticColors.Instance.lightBlue;
                _lineRenderer.endColor = StaticColors.Instance.lightBlue;
                break;
            case InteractorState.InteractionAvailable:
                _lineRenderer.startColor = StaticColors.Instance.tangerine;
                _lineRenderer.endColor = StaticColors.Instance.tangerine;
                break;
            case InteractorState.InteractionLocked:
                _lineRenderer.startColor = Color.red;
                _lineRenderer.endColor = Color.red;
                break;
            case InteractorState.Grabbing:
                _lineRenderer.startColor = StaticColors.Instance.lightBlue;
                _lineRenderer.endColor = StaticColors.Instance.lightBlue;
                break;
        }
    }
}

