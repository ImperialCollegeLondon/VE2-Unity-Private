using UnityEngine;
using ViRSE.Core.VComponents;

public interface IRaycastProvider
{
    bool Raycast(Vector3 rayOrigin, Vector3 raycastDirection, out IRaycastResultWrapper hit, float maxRaycastDistance, LayerMask layerMask);
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
    public virtual bool Raycast(Vector3 rayOrigin, Vector3 raycastDirection, out IRaycastResultWrapper result, float maxRaycastDistance, LayerMask layerMask) 
    {
        bool hitSomething = Physics.Raycast(rayOrigin, raycastDirection, out RaycastHit raycastHit, maxRaycastDistance, layerMask);
        result = hitSomething? new RaycastResultWrapper(raycastHit) : null;
        return hitSomething;
    }
}

public interface IRaycastResultWrapper 
{
    public Collider Collider { get; }
    public GameObject GameObject { get; }
    public IRangedPlayerInteractableIntegrator RangedInteractableHit { get; }
    public Vector3 Point { get; }
}

public class RaycastResultWrapper : IRaycastResultWrapper
{
    public Collider Collider => hit.collider;
    public GameObject GameObject => Collider.gameObject;
    public IRangedPlayerInteractableIntegrator RangedInteractableHit => GameObject.GetComponent<IRangedPlayerInteractableIntegrator>(); //Null reference here
    public Vector3 Point => hit.point;    
    private RaycastHit hit;

    public RaycastResultWrapper(RaycastHit raycastHit)
    {
        hit = raycastHit;
    }

    public RaycastResultWrapper()
    {
        hit = new RaycastHit();
    }
}
