using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using VE2.Core.Player;
using VE2.Core.Player.InteractionFinders;
using VE2.Core.VComponents.InteractableInterfaces;

public class InteractorVR : PointerInteractor
{
    private V_CollisionDetector _collisionDetector;
    private LineRenderer _lineRenderer;
    private Material _lineMaterial;
    private const float LINE_EMISSION_INTENSITY = 15;

    public void InitializeVR(Transform rayOrigin, InteractorType interactorType, IMultiplayerSupport multiplayerSupport, InteractorInputContainer interactorInputContainer, IRaycastProvider raycastProvider,
        V_CollisionDetector collisionDetector, LineRenderer lineRenderer) 
    {
        base.Initialize(rayOrigin, interactorType, multiplayerSupport, interactorInputContainer, raycastProvider);
        _collisionDetector = collisionDetector;
        _lineRenderer = lineRenderer;
        _lineMaterial = _lineRenderer.material;
        _lineMaterial.EnableKeyword("_EMISSION");
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

    protected override void HandleRaycastDistance(float distance)
    {
        _lineRenderer.SetPosition(1, new Vector3(0, 0, distance / _lineRenderer.transform.lossyScale.z));
    }

    protected override void SetInteractorState(InteractorState newState)
    {
        _lineRenderer.enabled = newState != InteractorState.Grabbing;

        switch (newState)
        {
            case InteractorState.Idle:
                _lineMaterial.color = StaticColors.Instance.lightBlue;
                _lineMaterial.SetColor("_EmissionColor", StaticColors.Instance.lightBlue * LINE_EMISSION_INTENSITY);
                break;
            case InteractorState.InteractionAvailable:

                _lineMaterial.color = StaticColors.Instance.tangerine;
                _lineMaterial.SetColor("_EmissionColor", StaticColors.Instance.tangerine * LINE_EMISSION_INTENSITY);
                break;
            case InteractorState.InteractionLocked:

                _lineMaterial.color = Color.red;
                _lineMaterial.SetColor("_EmissionColor", Color.red * LINE_EMISSION_INTENSITY);
                break;
            case InteractorState.Grabbing:

                break;
        }
    }
}

