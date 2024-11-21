using UnityEngine;
using VE2.Core.VComponents.InteractableFindables;
using VE2.Core.VComponents.InteractableInterfaces;

//TODO: - namespace!

public interface IRaycastProvider
{
    public RaycastResultWrapper Raycast(Vector3 rayOrigin, Vector3 raycastDirection, float maxRaycastDistance, LayerMask layerMask);
}

public class RaycastProvider : IRaycastProvider
{
    public RaycastResultWrapper Raycast(Vector3 rayOrigin, Vector3 raycastDirection, float maxRaycastDistance, LayerMask layerMask) 
    {
        RaycastResultWrapper result;

        if (Physics.Raycast(rayOrigin, raycastDirection, out RaycastHit raycastHit, maxRaycastDistance, layerMask)) 
        {
            //Debug.Log("Raycast hit: " + raycastHit.collider.name + " - pos " + raycastHit.point);
            if (raycastHit.collider.TryGetComponent(out IRangedPlayerInteractableIntegrator rangedPlayerInteractableIntegrator)) 
            {
                result = new(rangedPlayerInteractableIntegrator.RangedInteractionModule, raycastHit.distance);
            }
            else 
                result = new(null, raycastHit.distance);
        }
        else 
        {
            result = new(null, maxRaycastDistance);
        }

        return result;
    }
}

public class RaycastResultWrapper 
{
    public IRangedInteractionModule RangedInteractable { get; private set; }
    public float HitDistance { get; private set; }
    public bool HitInteractable => RangedInteractable != null;
    public bool RangedInteractableIsInRange => RangedInteractable != null && HitDistance <= RangedInteractable.InteractRange;

    public RaycastResultWrapper(IRangedInteractionModule rangedInteractable, float distance) 
    {
        RangedInteractable = rangedInteractable;
        HitDistance = distance;
    }
}
