using System;
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

        public RaycastResultWrapper SphereCastAll(Vector3 rayOrigin, float sphereRadius, Vector3 raycastDirection, float maxRaycastDistance, LayerMask layerMask, bool failsafeGrab = false, Vector3 palmDir = default);
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
                        result = new(rangedInteractionModuleProvider.RangedInteractionModule, null, raycastHit.distance, raycastHit.transform.position);
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

        public RaycastResultWrapper SphereCastAll(Vector3 rayOrigin, float sphereRadius, Vector3 raycastDirection, float maxRaycastDistance, LayerMask layerMask, bool failsafeGrab = false, Vector3 palmDir = default)
        {
            RaycastResultWrapper result;

            RaycastHit[] hits = Physics.SphereCastAll(rayOrigin, sphereRadius, raycastDirection, maxRaycastDistance, layerMask);

            if (hits.Length > 0)
            {
                IRangedGrabInteractionModuleProvider closestRangedGrabInteractionProvider = null;
                float closestDistance = float.MaxValue;
                Vector3 closestHitPoint = Vector3.zero;

                foreach (var hit in hits)
                {
                    Transform currentTransform = hit.collider.transform;
                    IRangedGrabInteractionModuleProvider rangedGrabInteractionModuleProvider = null;

                    while (currentTransform != null)
                    {
                        if (currentTransform.TryGetComponent(out rangedGrabInteractionModuleProvider))
                            break;

                        currentTransform = currentTransform.parent;
                    }

                    if (rangedGrabInteractionModuleProvider != null)
                    {
                        if(!rangedGrabInteractionModuleProvider.RangedGrabInteractionModule.VrRaySnap)
                            continue;
                    
                        float VRRaySnapRange = rangedGrabInteractionModuleProvider.RangedGrabInteractionModule.VRRaySnapRange;
                        float VRRaySnapRangeBackOfHand = rangedGrabInteractionModuleProvider.RangedGrabInteractionModule.VRRaySnapRangeBackOfHand;
                        float GrabMultiplier = failsafeGrab ? rangedGrabInteractionModuleProvider.RangedGrabInteractionModule.FailsafeGrabMultiplier : 1f;
                        Vector3 grabbablePosition = rangedGrabInteractionModuleProvider.RangedGrabInteractionModule.AttachPoint.position; //so ray snaps to the grabbable's attach point

                        float distanceFromGrabbable = Vector3.Distance(rayOrigin, grabbablePosition);
                        bool isOnPalm = Vector3.Angle(grabbablePosition - rayOrigin, palmDir) < 90f;

                        if(distanceFromGrabbable > VRRaySnapRangeBackOfHand * GrabMultiplier && !isOnPalm)
                            continue;

                        //check if facing the palm, the grabbable is within the failsafe range
                        //or if not facing palm, the grabbable is within the failsafe range and closer than the closest distance
                        if (distanceFromGrabbable <= VRRaySnapRange * GrabMultiplier
                            && distanceFromGrabbable < closestDistance)
                        {
                            closestHitPoint = grabbablePosition;
                            closestRangedGrabInteractionProvider = rangedGrabInteractionModuleProvider;
                            closestDistance = distanceFromGrabbable;
                        }
                    }
                }

                if (closestRangedGrabInteractionProvider != null)
                {
                    result = new(closestRangedGrabInteractionProvider.RangedInteractionModule, null, closestDistance, closestHitPoint);
                }
                else
                {
                    result = new(null, null, maxRaycastDistance);
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
        public Vector3 HitPosition { get; private set; }
        public bool HitInteractableOrUI => HitInteractable || HitUIButton;
        public bool HitInteractable => RangedInteractable != null;
        public bool HitInteractableInRange => HitInteractable && RangedInteractableIsInRange;
        public bool HitUIButton => UIButton != null;
        public bool RangedInteractableIsInRange => RangedInteractable != null && HitDistance <= RangedInteractable.InteractRange;
        public bool HitScrollableInteractableInRange => HitInteractableInRange && RangedInteractable is IRangedAdjustableInteractionModule;
        public bool HitScrollableUI => HitUIButton && false /*TODO: replace w/ && UIButton is IScrollableUI*/;

        public RaycastResultWrapper(IRangedInteractionModule rangedInteractable, Button uiButton, float distance, Vector3 hitPosition = default)
        {
            HitPosition = hitPosition;
            RangedInteractable = rangedInteractable;
            UIButton = uiButton;
            HitDistance = distance;
        }
    }
}
