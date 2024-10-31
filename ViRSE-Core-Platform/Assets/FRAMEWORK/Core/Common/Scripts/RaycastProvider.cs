using UnityEngine;
using ViRSE.Core.VComponents;

public interface IRaycastProvider
{
    bool TryGetRangedPlayerInteractable(Vector3 rayOrigin, Vector3 raycastDirection, out RangedPlayerInteractableHitResult hit, float maxRaycastDistance, LayerMask layerMask);
}

public class RaycastProvider : IRaycastProvider
{
    public bool TryGetRangedPlayerInteractable(Vector3 rayOrigin, Vector3 raycastDirection, out RangedPlayerInteractableHitResult result, float maxRaycastDistance, LayerMask layerMask) 
    {
        if (Physics.Raycast(rayOrigin, raycastDirection, out RaycastHit raycastHit, maxRaycastDistance, layerMask)) 
        {
            if (raycastHit.collider.gameObject.TryGetComponent(out IRangedPlayerInteractableIntegrator rangedInteractableIntegrator))
            {
                float distance = Vector3.Distance(rayOrigin, raycastHit.transform.position);
                result = new RangedPlayerInteractableHitResult(rangedInteractableIntegrator.RangedPlayerInteractable, distance);
                return true;
            }
        }

        result = null;
        return false;
    }
}

public class RangedPlayerInteractableHitResult 
{
    public IRangedPlayerInteractable RangedPlayerInteractable { get; private set; }
    public float Distance { get; private set; }

    public RangedPlayerInteractableHitResult(IRangedPlayerInteractable rangedPlayerInteractable, float distance) 
    {
        RangedPlayerInteractable = rangedPlayerInteractable;
        Distance = distance;
    }
}
