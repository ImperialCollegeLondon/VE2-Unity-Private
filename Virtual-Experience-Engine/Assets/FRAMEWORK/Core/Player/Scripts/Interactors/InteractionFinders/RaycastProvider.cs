using UnityEngine;
using VE2.Core.VComponents.InteractableFindables;
using VE2.Core.VComponents.InteractableInterfaces;

//TODO: - namespace!

public interface IRaycastProvider
{
    bool TryGetRangedInteractionModule(Vector3 rayOrigin, Vector3 raycastDirection, out RaycastResultWrapper hit, float maxRaycastDistance, LayerMask layerMask);
}

public class RaycastProvider : IRaycastProvider
{
    public bool TryGetRangedInteractionModule(Vector3 rayOrigin, Vector3 raycastDirection, out RaycastResultWrapper result, float maxRaycastDistance, LayerMask layerMask) 
    {
        if (Physics.Raycast(rayOrigin, raycastDirection, out RaycastHit raycastHit, maxRaycastDistance, layerMask)) 
        {
            if (raycastHit.collider.TryGetComponent(out IRangedPlayerInteractableIntegrator rangedPlayerInteractableIntegrator)) 
            {
                result = new(rangedPlayerInteractableIntegrator.RangedInteractionModule, raycastHit.distance);
                return true;
            }
        }

        result = null;
        return false;
    }
}

public class RaycastResultWrapper 
{
    public IRangedInteractionModule RangedInteractable { get; private set; }
    public float Distance { get; private set; }

    public RaycastResultWrapper(IRangedInteractionModule rangedInteractable, float distance) 
    {
        RangedInteractable = rangedInteractable;
        Distance = distance;
    }
}
