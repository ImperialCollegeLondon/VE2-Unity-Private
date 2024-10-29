using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using ViRSE.Core.Shared;
using ViRSE.Core.VComponents;

namespace ViRSE.Core.Player
{
    public class Interactor2D : MonoBehaviour
    {
        //TODO - a bunch of this should go into a superclass 
        public Transform GrabberTransform;
        private RaycastProvider _raycaster;

        private Transform rayOrigin;
        private float maxRaycastDistance;
        [SerializeField] private LayerMask layerMask; // Add a layer mask field

        [SerializeField] private Image reticuleImage;
        [SerializeField] /*[ReadOnly]*/ private string raycastHitDebug;

        private IRangedPlayerInteractableIntegrator _hoveringRangedInteractable = null;

        private InteractorID _interactorID => new(_multiplayerSupport == null? 0 : _multiplayerSupport.LocalClientID, InteractorType.TwoD);
        private InputHandler2D _inputHandler2D => V_InputHandler.Instance.InputHandler2D;
        private IRaycastProvider _raycastProvider => RaycastProvider.Instance;

        private IMultiplayerSupport _multiplayerSupport;
        private bool _waitingForMultiplayerSupport => _multiplayerSupport != null && !_multiplayerSupport.IsConnectedToServer;

        // Setup method to initialize the ray origin and max raycast distance
        public void Initialize(Camera camera2d, IMultiplayerSupport multiplayerSupport)
        {
            rayOrigin = camera2d.transform;
            maxRaycastDistance = camera2d.farClipPlane;
            _multiplayerSupport = multiplayerSupport;
        }

        private void OnEnable()
        {
            _inputHandler2D.OnMouseLeftClick += HandleLeftClick;
        }

        private void OnDisable()
        {
            _inputHandler2D.OnMouseLeftClick -= HandleLeftClick;
        }

        private void HandleLeftClick()
        {
            if (_hoveringRangedInteractable != null)
            {
                if (_hoveringRangedInteractable is IRangedClickPlayerInteractableIntegrator rangedClickInteractable)
                    rangedClickInteractable.InvokeOnClickDown(_interactorID);
            }
        }

        void Update()
        {
            //If we're waiting for multiplayer, we can't interact with anything 
            //We need to know our ID before we can interact with anything, as this ID will end up in the state module
            if (_waitingForMultiplayerSupport)
                return;

            _hoveringRangedInteractable = null;

            // Perform the raycast using the layer mask
            if (_raycastProvider.Raycast(rayOrigin.position, rayOrigin.transform.forward, out IRaycastResultWrapper hitWrapper, maxRaycastDistance, layerMask))
            {
                Vector3 hitPoint = hitWrapper.Point;
                Collider hitCollider = hitWrapper.Collider;
                IRangedPlayerInteractableIntegrator rangedInteractable = hitWrapper.RangedInteractableHit;

                if (rangedInteractable != null && !rangedInteractable.AdminOnly)
                {
                    float distance = Vector3.Distance(rayOrigin.transform.position, hitCollider.transform.position);
                    if (distance <= rangedInteractable.InteractRange)
                    {
                        _hoveringRangedInteractable = rangedInteractable;
                        raycastHitDebug = rangedInteractable.ToString();
                    }
                }
                else
                {
                    raycastHitDebug = hitCollider.gameObject.name;
                }
            }
            else
            {
                raycastHitDebug = "none";
            }

            reticuleImage.color = _hoveringRangedInteractable != null ? StaticColors.Instance.tangerine : StaticColors.Instance.lightBlue;
        }
    }
}
