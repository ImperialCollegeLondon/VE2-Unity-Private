using System.Collections.Generic;
using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.Core.Player
{
    //TODO: DOesn't need to be a MB?
    //If not an MB, we need to figure out how the state module will find the interactor!!
    //Through the ServiceLocator, maybe? Hmmn, needs to find remote interactors too though, maybe MB is actually fine here
    //Actually, maybe we have some base PointerInteractor that can hold some lookup table or something? 
    //Nah, let's have an InteractorContainer in the ServiceLocator, each interactor will register itself there
    public abstract class PointerInteractor : MonoBehaviour, IInteractor
    {
        public Transform Transform => transform;

        [SerializeField] public Transform GrabberTransform;
        [SerializeField] protected LayerMask _LayerMask; // Add a layer mask field
        [SerializeField] protected string _RaycastHitDebug;

        protected const float MAX_RAYCAST_DISTANCE = 10;
        protected IRangedGrabInteractionModule _CurrentGrabbingGrabbable;
        protected bool IsCurrentlyGrabbing => _CurrentGrabbingGrabbable != null;
        protected InteractorID _InteractorID => new(_multiplayerSupport == null ? (ushort)0 : _multiplayerSupport.LocalClientID, _InteractorType);
        protected bool _WaitingForMultiplayerSupport => _multiplayerSupport != null && !_multiplayerSupport.IsConnectedToServer;


        //TODO: maybe these can all be private?
        protected Transform _RayOrigin;
        protected InteractorType _InteractorType;
        protected IMultiplayerSupport _multiplayerSupport;
        private InteractorInputContainer _interactorInputContainer; 
        protected IRaycastProvider _RaycastProvider;

        private ushort _randomID;

        // Setup method to initialize the ray origin and max raycast distance
        //TODO: Ideally, this would be a constructor
        public void Initialize(Transform rayOrigin, InteractorType interactorType, IMultiplayerSupport multiplayerSupport, InteractorInputContainer interactorInputContainer, IRaycastProvider raycastProvider)
        {
            _RayOrigin = rayOrigin;
            _InteractorType = interactorType;
            _multiplayerSupport = multiplayerSupport;
            _interactorInputContainer = interactorInputContainer;
            _RaycastProvider = raycastProvider;

            _randomID = (ushort)Random.Range(0, ushort.MaxValue);

            if (_WaitingForMultiplayerSupport)
                _multiplayerSupport.OnConnectedToInstance += RenameInteractorToLocalID;
            else
                RenameInteractorToLocalID();
        }

        public virtual void HandleOnEnable()
        {
            Debug.Log("Random ID: " + _randomID + " subscrived to input");

            _interactorInputContainer.RangedClick.OnPressed += HandleRangedClickPressed;
            _interactorInputContainer.HandheldClick.OnPressed += HandleHandheldClickPressed;
            _interactorInputContainer.Grab.OnPressed += HandleGrabPressed;
            _interactorInputContainer.ScrollTickUp.OnTickOver += HandleScrollUp;
            _interactorInputContainer.ScrollTickDown.OnTickOver += HandleScrollDown;
        }

        public virtual void HandleOnDisable()
        {
            Debug.Log("Random ID: " + _randomID + " unsubscribed from input");

            _interactorInputContainer.RangedClick.OnPressed -= HandleRangedClickPressed;
            _interactorInputContainer.HandheldClick.OnPressed -= HandleHandheldClickPressed;
            _interactorInputContainer.Grab.OnPressed -= HandleGrabPressed;
            _interactorInputContainer.ScrollTickUp.OnTickOver -= HandleScrollUp;
            _interactorInputContainer.ScrollTickDown.OnTickOver -= HandleScrollDown;
        }

        private void RenameInteractorToLocalID() 
        {
            ushort localID = 0;

            if (_multiplayerSupport != null)
            {
                _multiplayerSupport.OnConnectedToInstance -= RenameInteractorToLocalID;
                localID = _multiplayerSupport.LocalClientID;
            }

            gameObject.name = $"Interactor{localID}-{_InteractorType}";
        }

        void Update()
        {
            if (IsCurrentlyGrabbing)
                return;

            RaycastResultWrapper raycastResultWrapper = GetRayCastResult();

            if (!_WaitingForMultiplayerSupport && raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange)
            {
                bool isAllowedToInteract = !raycastResultWrapper.RangedInteractable.AdminOnly;
                SetInteractorState(isAllowedToInteract ? InteractorState.InteractionAvailable : InteractorState.InteractionLocked);
                _RaycastHitDebug = raycastResultWrapper.RangedInteractable.ToString();
            }
            else
            {
                SetInteractorState(InteractorState.Idle);
                _RaycastHitDebug = "none";
            }

            HandleRaycastDistance(raycastResultWrapper.HitDistance);
        }

        protected virtual void HandleRaycastDistance(float distance) { } //TODO: Code smell? InteractorVR needs this to set the LineRenderer length

        private RaycastResultWrapper GetRayCastResult() 
        {
            return _RaycastProvider.Raycast(_RayOrigin.position, _RayOrigin.forward, MAX_RAYCAST_DISTANCE, _LayerMask);
        }    

        private void HandleRangedClickPressed()
        {
            RaycastResultWrapper raycastResultWrapper = GetRayCastResult();

            if (!_WaitingForMultiplayerSupport && !IsCurrentlyGrabbing && raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange)
            {
                if (raycastResultWrapper.RangedInteractable is IRangedClickInteractionModule rangedClickInteractable)
                    rangedClickInteractable.Click(_InteractorID.ClientID);
            }
        }

        private void HandleGrabPressed()
        {
            if (IsCurrentlyGrabbing)
            {
                IRangedGrabInteractionModule rangedGrabInteractableToDrop = _CurrentGrabbingGrabbable;
                rangedGrabInteractableToDrop.RequestLocalDrop(_InteractorID);
            }
            else 
            {
                RaycastResultWrapper raycastResultWrapper = GetRayCastResult();

                if (!_WaitingForMultiplayerSupport && raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange)
                {   
                    if (!raycastResultWrapper.RangedInteractable.AdminOnly)
                    {
                        if (raycastResultWrapper.RangedInteractable is IRangedGrabInteractionModule rangedGrabInteractable)
                        {
                            rangedGrabInteractable.RequestLocalGrab(_InteractorID);
                        }
                    }
                    else
                    {
                        //TODO, maybe play an error sound or something
                    }
                }
            }
        }

        public void ConfirmGrab(IRangedGrabInteractionModule rangedGrabInteractable)
        {
            _CurrentGrabbingGrabbable = rangedGrabInteractable;
            SetInteractorState(InteractorState.Grabbing);
        }

        public void ConfirmDrop()
        {
            SetInteractorState(InteractorState.Idle);
            _CurrentGrabbingGrabbable = null;
        }

        private void HandleHandheldClickPressed()
        {
            if (!_WaitingForMultiplayerSupport && IsCurrentlyGrabbing)
            {
                foreach (IHandheldInteractionModule handheldInteraction in _CurrentGrabbingGrabbable.HandheldInteractions)
                {
                    if (handheldInteraction is IHandheldClickInteractionModule handheldClickInteraction)
                    {
                        handheldClickInteraction.Click(_InteractorID.ClientID);
                    }
                }
            }
        }

        private void HandleScrollUp()
        {
            if (!IsCurrentlyGrabbing)
                return;

            foreach (IHandheldInteractionModule handheldInteraction in _CurrentGrabbingGrabbable.HandheldInteractions)
            {
                if (handheldInteraction is IHandheldScrollInteractionModule handheldScrollInteraction)
                {
                    handheldScrollInteraction.ScrollUp(_InteractorID.ClientID);
                }
            }
        }
        private void HandleScrollDown()
        {
            if (!IsCurrentlyGrabbing)
                return;

            foreach (IHandheldInteractionModule handheldInteraction in _CurrentGrabbingGrabbable.HandheldInteractions)
            {
                if (handheldInteraction is IHandheldScrollInteractionModule handheldScrollInteraction)
                {
                    handheldScrollInteraction.ScrollDown(_InteractorID.ClientID);
                }
            }
        }

        // protected bool TryGetHoveringRangedInteractable(out IRangedInteractionModule hoveringInteractable)
        // {
        //     // Perform the raycast using the layer mask
        //     if (!_WaitingForMultiplayerSupport && _RaycastProvider.Raycast(_RayOrigin.position, _RayOrigin.transform.forward, MAX_RAYCAST_DISTANCE, _LayerMask))
        //     {
        //         if (rangedInteractableHitResult.HitDistance <= rangedInteractableHitResult.RangedInteractable.InteractRange)
        //         {
        //             hoveringInteractable = rangedInteractableHitResult.RangedInteractable;
        //             return true;
        //         }
        //     }

        //     hoveringInteractable = null;
        //     return false;
        // }


        protected abstract void SetInteractorState(InteractorState newState);

        protected enum InteractorState
        {
            Idle,
            InteractionAvailable,
            InteractionLocked,
            Grabbing
        }
    }
}

