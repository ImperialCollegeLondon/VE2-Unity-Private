using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VE2.Core.VComponents.API;

namespace VE2.Core.Player.Internal
{
    internal interface IRaycastProvider
    {
        public RaycastResultWrapper Raycast(Vector3 rayOrigin, Vector3 raycastDirection, float maxRaycastDistance, LayerMask layerMask);
    }

    internal class RaycastProvider : IRaycastProvider
    {
        public RaycastResultWrapper Raycast(Vector3 rayOrigin, Vector3 raycastDirection, float maxRaycastDistance, LayerMask layerMask) 
        {
            RaycastResultWrapper result;

            if (Physics.Raycast(rayOrigin, raycastDirection, out RaycastHit raycastHit, maxRaycastDistance, layerMask)) 
            {
                //ProcessUIHover(raycastHit.collider.gameObject);
                Button button = GetUIButton(raycastHit);

                if (button != null) 
                {
                    result = new(null, button, raycastHit.distance);   
                }
                else //Search up through the heirarchy looking for 
                {
                    Transform currentTransform = raycastHit.collider.transform;
                    IRangedInteractionModuleProvider rangedInteractionModuleProvider = null;

                    while (currentTransform != null) 
                    {
                        if (currentTransform.TryGetComponent(out rangedInteractionModuleProvider)) 
                            break;

                        currentTransform = currentTransform.parent;
                    }

                    if (rangedInteractionModuleProvider != null) 
                    {
                        result = new(rangedInteractionModuleProvider.RangedInteractionModule, null, raycastHit.distance);
                    }
                    else 
                    {
                        result = new(null, null, raycastHit.distance);
                    }
                }
            }
            else 
            {
                result = new(null, null, maxRaycastDistance);
            }

            return result;
        }

        private Button GetUIButton(RaycastHit hit)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);

            // Convert the hit point to screen space
            Camera camera = Camera.main; // Or assign your specific camera
            pointerData.position = camera.WorldToScreenPoint(hit.point); // Convert to screen space

            // Perform the raycast against the UI
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                if (result.gameObject.TryGetComponent(out Button button)) 
                    return button;
            }

            return null;
        }
    }

    internal class RaycastResultWrapper 
    {
        public IRangedInteractionModule RangedInteractable { get; private set; }
        public IRangedInteractionModule RangedInteractableInRange => HitInteractableInRange ? RangedInteractable : null;
        public Button UIButton;
        public float HitDistance { get; private set; }
        public bool HitInteractableOrUI => HitInteractable || HitUIButton;
        public bool HitInteractable => RangedInteractable != null;
        public bool HitInteractableInRange => HitInteractable && RangedInteractableIsInRange;
        public bool HitUIButton => UIButton != null;
        public bool RangedInteractableIsInRange => RangedInteractable != null && HitDistance <= RangedInteractable.InteractRange;
        public bool HitScrollableInteractableInRange => HitInteractableInRange && RangedInteractable is IRangedAdjustableInteractionModule;
        public bool HitScrollableUI => HitUIButton && false /*TODO: replace w/ && UIButton is IScrollableUI*/;

        public RaycastResultWrapper(IRangedInteractionModule rangedInteractable, Button uiButton, float distance) 
        {
            RangedInteractable = rangedInteractable;
            UIButton = uiButton;
            HitDistance = distance;
        }
    }
}
