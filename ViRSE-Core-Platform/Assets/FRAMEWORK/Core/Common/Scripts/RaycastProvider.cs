using UnityEngine;
using ViRSE.Core.VComponents;

public interface IRaycastProvider
{
    bool TryGetRangedPlayerInteractable(Vector3 rayOrigin, Vector3 raycastDirection, out IRangedPlayerInteractableImplementor hit, float maxRaycastDistance, LayerMask layerMask);
}

public class RaycastProvider : IRaycastProvider
{
    public bool TryGetRangedPlayerInteractable(Vector3 rayOrigin, Vector3 raycastDirection, out IRangedPlayerInteractableImplementor result, float maxRaycastDistance, LayerMask layerMask) 
    {
        if (Physics.Raycast(rayOrigin, raycastDirection, out RaycastHit raycastHit, maxRaycastDistance, layerMask)) 
        {
            if (raycastHit.collider.gameObject.TryGetComponent(out IRangedPlayerInteractableIntegrator rangedInteractableIntegrator))
            {
                float distance = Vector3.Distance(rayOrigin, raycastHit.transform.position);
                if (distance <= rangedInteractableIntegrator.RangedPlayerInteractableImplementor.InteractRange)
                {
                    result = rangedInteractableIntegrator.RangedPlayerInteractableImplementor;
                    return true;
                }
            }
        }


        result = null;
        return false;
    }
}
