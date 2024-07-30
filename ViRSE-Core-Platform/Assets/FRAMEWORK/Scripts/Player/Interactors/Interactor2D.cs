using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class Interactor2D : MonoBehaviour
{
    private Transform rayOrigin;
    private float maxRaycastDistance;
    [SerializeField] private LayerMask layerMask; // Add a layer mask field

    [SerializeField] private Image reticuleImage;
    [SerializeField][ReadOnly] private string raycastHitDebug;

    // Setup method to initialize the ray origin and max raycast distance
    public void Setup(Camera camera2d)
    {
        rayOrigin = camera2d.transform;
        maxRaycastDistance = camera2d.farClipPlane;
    }

    void Update()
    {
        bool foundRangedInteractable = false;

        // Perform the raycast using the layer mask
        if (Physics.Raycast(rayOrigin.position, rayOrigin.transform.forward, out RaycastHit hit, maxRaycastDistance, layerMask))
        {
            Vector3 hitPoint = hit.point;
            Collider hitCollider = hit.collider;

            if (hit.collider.TryGetComponent(out IRangedInteractableComponent rangedInteractionComponent) &&
                rangedInteractionComponent.InteractionModule.IsPositionWithinInteractRange(rayOrigin.position))
            {
                foundRangedInteractable = true;
                raycastHitDebug = rangedInteractionComponent.InteractionModule.ToString();
            }
            else
            {
                raycastHitDebug = hit.collider.gameObject.name;
            }
        }
        else
        {
            raycastHitDebug = "none";
        }

        reticuleImage.color = foundRangedInteractable ? StaticColors.instance.tangerine : StaticColors.instance.lightBlue;
    }
}


//We could have some "InteractableComponent" interface that would return a generic reference to the interaction interface? 
/* What actually are the interaction interfaces here? 
 * Ranged grab 
 * Ranged click 
 * Collide 
 * 
 * Handheld click 
 * Handheld scroll
 * 
 * So we could just have an interface that returns generic ones of these 
 */






//In VR, we want this line to be drawn each and every frame, which means the VR interactor needs to know how far to draw this ray

//Each interactor needs to have something for "OnHitXYZ"
//maybe this should be its own component?

//So in 2D, we need to change the reticule colour, and show a tooltip (maybe)
//in VR, we need to change the ray colour, and show a tooltip 

//Do we even need to change the reticule colour based on what we actually hit? Probably not...
//But we DO need to change the tooltip based on what we hit! 


//Maybe a tooltip handler script? We pass it the VC that we hit, and the TTHandler works out if it should show tooltips, and which one 