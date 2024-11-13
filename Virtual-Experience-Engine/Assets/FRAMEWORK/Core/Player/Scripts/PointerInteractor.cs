using System.Collections.Generic;
using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.Core.Player
{
    public abstract class PointerInteractor : MonoBehaviour, IInteractor
    {
        [SerializeField] public Transform GrabberTransform;
        [SerializeField] protected LayerMask _LayerMask; // Add a layer mask field
        [SerializeField] protected string _RaycastHitDebug;

        protected Transform _RayOrigin;
        protected const float MAX_RAYCAST_DISTANCE = 10;
        protected IRangedGrabInteractionModule _CurrentGrabbingGrabbable;
        protected InteractorID _InteractorID => new(_multiplayerSupport == null ? (ushort)0 : _multiplayerSupport.LocalClientID, InteractorType);
        private bool _waitingForMultiplayerSupport => _multiplayerSupport != null && !_multiplayerSupport.IsConnectedToServer;

        protected abstract InteractorType InteractorType { get; }

        protected IMultiplayerSupport _multiplayerSupport;
        protected IInputHandler _InputHandler;
        protected IRaycastProvider _RaycastProvider;

        // Setup method to initialize the ray origin and max raycast distance
        public void Initialize(Camera camera2d, IMultiplayerSupport multiplayerSupport, IInputHandler inputHandler, IRaycastProvider raycastProvider)
        {
            _RayOrigin = camera2d.transform;

            _multiplayerSupport = multiplayerSupport;
            _InputHandler = inputHandler;
            _RaycastProvider = raycastProvider;

            if (_multiplayerSupport != null && !_multiplayerSupport.IsConnectedToServer)
                _multiplayerSupport.OnConnectedToServer += RenameInteractorToLocalID;
            else
                RenameInteractorToLocalID();

            SubscribeToInputHandler(_InputHandler);
        }

        private void RenameInteractorToLocalID() 
        {
            ushort localID = 0;

            if (_multiplayerSupport != null)
            {
                _multiplayerSupport.OnConnectedToServer -= RenameInteractorToLocalID;
                localID = _multiplayerSupport.LocalClientID;
            }

            gameObject.name = $"Interactor{localID}-{InteractorType}";
        }

        protected abstract void SubscribeToInputHandler(IInputHandler inputHandler);

        protected bool TryGetHoveringRangedInteractable(out IRangedInteractionModule hoveringInteractable)
        {
            // Perform the raycast using the layer mask
            if (!_waitingForMultiplayerSupport && _RaycastProvider.TryGetRangedInteractionModule(_RayOrigin.position, _RayOrigin.transform.forward, out RaycastResultWrapper rangedInteractableHitResult, MAX_RAYCAST_DISTANCE, _LayerMask))
            {
                if (rangedInteractableHitResult.Distance <= rangedInteractableHitResult.RangedInteractable.InteractRange)
                {
                    hoveringInteractable = rangedInteractableHitResult.RangedInteractable;
                    return true;
                }
            }

            hoveringInteractable = null;
            return false;
        }

        private void OnDestroy()
        {
            UnsubscribeFromInputHandler(_InputHandler);
        }

        protected abstract void UnsubscribeFromInputHandler(IInputHandler inputHandler);

        abstract public Transform ConfirmGrab(IRangedGrabInteractionModule rangedGrabInteractable);

        abstract public void ConfirmDrop();
    }
}

