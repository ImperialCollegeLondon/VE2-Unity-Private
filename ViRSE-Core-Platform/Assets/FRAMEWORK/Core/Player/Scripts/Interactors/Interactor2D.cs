using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ViRSE.Core.VComponents;

namespace ViRSE.Core.Player
{
    public class Interactor2D : MonoBehaviour
    {
        //TODO - a bunch of this should go into a superclass 
        public Transform GrabberTransform;

        private Transform rayOrigin;
        private float maxRaycastDistance;
        [SerializeField] private LayerMask layerMask; // Add a layer mask field

        [SerializeField] private Image reticuleImage;
        [SerializeField] /*[ReadOnly]*/ private string raycastHitDebug;

        private IRangedPlayerInteractableImplementor _hoveringRangedInteractable = null;

        private InteractorID _interactorID;

        // Setup method to initialize the ray origin and max raycast distance
        public void Initialize(Camera camera2d)
        {
            rayOrigin = camera2d.transform;
            maxRaycastDistance = camera2d.farClipPlane;
            _interactorID = new InteractorID(0, InteractorType.TwoD);
        }

        private void OnEnable()
        {
            V_InputHandler.Instance.InputHandler2D.OnMouseLeftClick += HandleLeftClick;
        }

        private void OnDisable()
        {
            V_InputHandler.Instance.InputHandler2D.OnMouseLeftClick -= HandleLeftClick;
        }

        private void HandleLeftClick()
        {
            if (_hoveringRangedInteractable != null)
            {
                if (_hoveringRangedInteractable is IRangedClickPlayerInteractableImplementor rangedClickInteractable)
                    rangedClickInteractable.InvokeOnClickDown(_interactorID);
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
