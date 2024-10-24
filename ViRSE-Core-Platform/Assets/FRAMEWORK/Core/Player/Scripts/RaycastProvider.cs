using UnityEngine;
using ViRSE.Core.VComponents;

public interface IRaycastProvider
{
    bool Raycast(Vector3 rayOrigin, Vector3 raycastDirection, out RaycastResultWrapper hit, float maxRaycastDistance, LayerMask layerMask);
}

public class RaycastProvider : IRaycastProvider
{
    protected static IRaycastProvider _instance;
    public static IRaycastProvider Instance
    {
        get
        {
            _instance ??= new RaycastProvider();
            return _instance;
        }
    }

    /// <summary>
    /// RaycastResultWrapper will be null if no hit is detected
    /// </summary>
    public virtual bool Raycast(Vector3 rayOrigin, Vector3 raycastDirection, out RaycastResultWrapper result, float maxRaycastDistance, LayerMask layerMask) 
    {
        bool hitSomething = Physics.Raycast(rayOrigin, raycastDirection, out RaycastHit raycastHit, maxRaycastDistance, layerMask);
        result = hitSomething? new RaycastResultWrapper(raycastHit) : null;
        return hitSomething;
    }
}

public class RaycastResultWrapper
{
    public Collider Collider => hit.collider;
    public GameObject GameObject => Collider.gameObject;
    public IRangedPlayerInteractableImplementor RangedInteractableHit => GameObject.GetComponent<IRangedPlayerInteractableImplementor>();
    public Vector3 Point => hit.point;    
    private RaycastHit hit;

    public RaycastResultWrapper(RaycastHit raycastHit)
    {
        hit = raycastHit;
    }
}
