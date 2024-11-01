using UnityEngine;
using ViRSE.Core.VComponents;
using ViRSE.Core.VComponents.PlayerInterfaces;

public interface IRaycastProvider
{
    bool TryGetGameObject(Vector3 rayOrigin, Vector3 raycastDirection, out RaycastResultWrapper hit, float maxRaycastDistance, LayerMask layerMask);
}

public class RaycastProvider : IRaycastProvider
{
    public bool TryGetGameObject(Vector3 rayOrigin, Vector3 raycastDirection, out RaycastResultWrapper result, float maxRaycastDistance, LayerMask layerMask) 
    {
        if (Physics.Raycast(rayOrigin, raycastDirection, out RaycastHit raycastHit, maxRaycastDistance, layerMask)) 
        {
            result = new RaycastResultWrapper(raycastHit.collider.gameObject, raycastHit.distance);
            return true;
        }
        else 
        {
            result = null;
            return false;
        }

    }
}

public class RaycastResultWrapper 
{
    public GameObject GameObject { get; private set; }
    public float Distance { get; private set; }

    public RaycastResultWrapper(GameObject gameObject, float distance) 
    {
        GameObject = gameObject;
        Distance = distance;
    }
}
