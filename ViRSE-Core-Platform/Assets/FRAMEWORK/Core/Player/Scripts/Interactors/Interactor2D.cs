using UnityEngine;
using UnityEngine.UI;
using ViRSE.FrameworkRuntime.LocalPlayerRig;
using ViRSE.PluginRuntime.VComponents;

namespace ViRSE.Core.Player
{
    public class Interactor2D : MonoBehaviour
    {
        private Transform rayOrigin;
        private float maxRaycastDistance;
        [SerializeField] private LayerMask layerMask; // Add a layer mask field

        [SerializeField] private Image reticuleImage;
        [SerializeField] /*[ReadOnly]*/ private string raycastHitDebug;

        private IRangedPlayerInteractableImplementor _hoveringRangedInteractable = null;

        private InteractorID _interactorID;

        // Setup method to initialize the ray origin and max raycast distance
        public void Setup(Camera camera2d)
        {
            rayOrigin = camera2d.transform;
            maxRaycastDistance = camera2d.farClipPlane;
            _interactorID = new InteractorID(0, InteractorType.TwoD);
        }

        //TODO - these need to be re-wired on domain reloadasd

        private void OnEnable()
        {
            InputHandler.Instance.OnMouseLeftClick.AddListener(HandleLeftClick);
        }

        private void OnDisable()
        {
            InputHandler.Instance.OnMouseLeftClick.RemoveListener(HandleLeftClick);
        }

        private void HandleLeftClick()
        {
            if (_hoveringRangedInteractable != null)
            {
                if (_hoveringRangedInteractable is IRangedClickPlayerInteractableImplementor rangedClickInteractable)
                {
                    rangedClickInteractable.InvokeOnClickDown(_interactorID);
                }
            }
        }

        void Update()
        {
            bool foundRangedInteractable = false;
            _hoveringRangedInteractable = null;

            // Perform the raycast using the layer mask
            if (Physics.Raycast(rayOrigin.position, rayOrigin.transform.forward, out RaycastHit hit, maxRaycastDistance, layerMask))
            {
                Vector3 hitPoint = hit.point;
                Collider hitCollider = hit.collider;

                if (hit.collider.TryGetComponent(out IRangedPlayerInteractableImplementor rangedInteractable) && !rangedInteractable.AdminOnly)
                {
                    float distance = Vector3.Distance(rayOrigin.transform.position, hit.collider.transform.position);
                    if (distance <= rangedInteractable.InteractRange)
                    {
                        foundRangedInteractable = true;
                        _hoveringRangedInteractable = rangedInteractable;
                        raycastHitDebug = rangedInteractable.ToString();
                    }
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

            reticuleImage.color = foundRangedInteractable ? StaticColors.Instance.tangerine : StaticColors.Instance.lightBlue;
        }
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



/*
 * So before, we were just storing the raycast, and then detecting a click down
 * Which raises the question, do we want to be using InputHandler for this? 
 * I guess? 
 * Ok, so we get a click from input handler, we can then either do a raycast and do the thing, or we can access whatever the current raycast result is? 
 * Let's go with the second one?
 */