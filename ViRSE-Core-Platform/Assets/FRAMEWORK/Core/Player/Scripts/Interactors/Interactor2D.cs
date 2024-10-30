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

        private InteractorID _interactorID => new(_multiplayerSupport == null? 0 : _multiplayerSupport.LocalClientID, InteractorType.TwoD);
        private IMultiplayerSupport _multiplayerSupport;
        private IInputHandler _inputHandler;
        private IRaycastProvider _raycastProvider;

        private bool _waitingForMultiplayerSupport => _multiplayerSupport != null && !_multiplayerSupport.IsConnectedToServer;

        // Setup method to initialize the ray origin and max raycast distance
        public void Initialize(Camera camera2d, IMultiplayerSupport multiplayerSupport, IInputHandler inputHandler, IRaycastProvider raycastProvider)
        {
            rayOrigin = camera2d.transform;
            maxRaycastDistance = camera2d.farClipPlane;

            _multiplayerSupport = multiplayerSupport;
            _inputHandler = inputHandler;
            _raycastProvider = raycastProvider;

            _inputHandler.OnMouseLeftClick += HandleLeftClick;
        }

        private void HandleLeftClick()
        {
            if (TryGetHoveringRangedInteractable(out IRangedPlayerInteractableImplementor hoveringInteractable))
            {
                if (hoveringInteractable is IRangedClickPlayerInteractableImplementor rangedClickInteractable)
                    rangedClickInteractable.InvokeOnClickDown(_interactorID);
            }
        }

        void Update()
        {
            if (TryGetHoveringRangedInteractable(out IRangedPlayerInteractableImplementor hoveringInteractable))
            {
                reticuleImage.color = StaticColors.Instance.tangerine;
                raycastHitDebug = hoveringInteractable.ToString();
            }
            else 
            {
                reticuleImage.color = StaticColors.Instance.lightBlue;
                raycastHitDebug = "none";
            }
        }

        private bool TryGetHoveringRangedInteractable(out IRangedPlayerInteractableImplementor hoveringInteractable)
        {
            // Perform the raycast using the layer mask
            if (!_waitingForMultiplayerSupport && _raycastProvider.TryGetRangedPlayerInteractable(rayOrigin.position, rayOrigin.transform.forward, out IRangedPlayerInteractableImplementor rangedInteractable, maxRaycastDistance, layerMask))
            {
                if (!rangedInteractable.AdminOnly)
                {
                    hoveringInteractable = rangedInteractable;
                    return true;
                }
            }

            hoveringInteractable = null;
            return false;
        }

        private void OnDestroy()
        {
            _inputHandler.OnMouseLeftClick -= HandleLeftClick;
        }
    }
}
