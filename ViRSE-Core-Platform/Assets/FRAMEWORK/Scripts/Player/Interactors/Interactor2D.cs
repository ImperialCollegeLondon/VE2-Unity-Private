using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Interactor2D : MonoBehaviour
{
    private Transform rayOrigin;
    private float maxRaycastDistance;

    [SerializeField] private Image reticuleImage;


    public void Setup(Camera camera2d)
    {
        rayOrigin = camera2d.transform;
        maxRaycastDistance = camera2d.farClipPlane;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        bool foundRangedInteractable = false;

        if (Physics.Raycast(rayOrigin.position, rayOrigin.transform.forward, out RaycastHit hit, maxRaycastDistance))
        {
            Vector3 hitPoint = hit.point;
            Collider hitCollider = hit.collider;

            if (hit.collider.TryGetComponent(out RangedInteractionModule rangedInteractionModule))
            {
                foundRangedInteractable = true;
            }
        }

        reticuleImage.color = foundRangedInteractable ? StaticColors.tangerine : StaticColors.lightBlue;
    }
}

//In VR, we want this line to be drawn each and every frame, which means the VR interactor needs to know how far to draw this ray

//Each interactor needs to have something for "OnHitXYZ"
//maybe this should be its own component?

//So in 2D, we need to change the reticule colour, and show a tooltip (maybe)
//in VR, we need to change the ray colour, and show a tooltip 

//Do we even need to change the reticule colour based on what we actually hit? Probably not...
//But we DO need to change the tooltip based on what we hit! 


//Maybe a tooltip handler script? We pass it the VC that we hit, and the TTHandler works out if it should show tooltips, and which one 