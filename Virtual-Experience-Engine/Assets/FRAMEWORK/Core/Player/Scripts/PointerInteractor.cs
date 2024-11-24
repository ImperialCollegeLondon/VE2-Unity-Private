using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.Core.Player
{
    public class InteractorReferences 
    {
        public Transform GrabberTransform => _grabberTransform;
        [SerializeField] private Transform _grabberTransform;

        public Transform RayOrigin => _rayOrigin;
        [SerializeField] private Transform _rayOrigin;

        public LayerMask LayerMask => _layerMask;
        [SerializeField] private LayerMask _layerMask;

        [SerializeField] private StringWrapper _raycastHitDebug;
        public StringWrapper RaycastHitDebug => _raycastHitDebug;
    }

    [Serializable]
    public class StringWrapper
    {
        [SerializeField, IgnoreParent] public string Value;
    }

    //TODO: DOesn't need to be a MB?
    //If not an MB, we need to figure out how the state module will find the interactor!!
    //Through the ServiceLocator, maybe? Hmmn, needs to find remote interactors too though, maybe MB is actually fine here
    //Actually, maybe we have some base PointerInteractor that can hold some lookup table or something? 
    //Nah, let's have an InteractorContainer in the ServiceLocator, each interactor will register itself there
    public abstract class PointerInteractor : IInteractor
    {
        public Transform GrabberTransform => _GrabberTransform;

        protected bool IsCurrentlyGrabbing => _CurrentGrabbingGrabbable != null;
        protected InteractorID _InteractorID => new(_multiplayerSupport == null ? (ushort)0 : _multiplayerSupport.LocalClientID, _InteractorType);
        protected bool _WaitingForMultiplayerSupport => _multiplayerSupport != null && !_multiplayerSupport.IsConnectedToServer;

        protected const float MAX_RAYCAST_DISTANCE = 10;
        protected IRangedGrabInteractionModule _CurrentGrabbingGrabbable;

        private readonly InteractorContainer _interactorContainer;
        private readonly InteractorInputContainer _interactorInputContainer;

        protected readonly Transform _GrabberTransform;
        protected readonly Transform _RayOrigin;
        private readonly LayerMask _layerMask;
        private readonly StringWrapper _raycastHitDebug;

        //TODO: maybe these can all be private?
        protected readonly InteractorType _InteractorType;
        protected readonly IRaycastProvider _RaycastProvider;
        protected readonly IMultiplayerSupport _multiplayerSupport;

        private ushort _randomIDTEMPDEBUG;

        // Setup method to initialize the ray origin and max raycast distance
        //TODO: Ideally, this would be a constructor
        public PointerInteractor(InteractorContainer interactorContainer, InteractorInputContainer interactorInputContainer,
            Transform grabberTransform, Transform rayOrigin, LayerMask layerMask, StringWrapper raycastHitDebug,
            InteractorType interactorType, IRaycastProvider raycastProvider, IMultiplayerSupport multiplayerSupport)
        {
            _interactorContainer = interactorContainer;
            _interactorInputContainer = interactorInputContainer;

            _GrabberTransform = grabberTransform;
            _RayOrigin = rayOrigin;
            _layerMask = layerMask;
            _raycastHitDebug = raycastHitDebug;

            _InteractorType = interactorType;
            _RaycastProvider = raycastProvider;
            _multiplayerSupport = multiplayerSupport;

            _randomIDTEMPDEBUG = (ushort)UnityEngine.Random.Range(0, ushort.MaxValue);
        }

        public virtual void HandleOnEnable()
        {
            Debug.Log("Random ID: " + _randomIDTEMPDEBUG + " subscrived to input");

            _interactorInputContainer.RangedClick.OnPressed += HandleRangedClickPressed;
            _interactorInputContainer.HandheldClick.OnPressed += HandleHandheldClickPressed;
            _interactorInputContainer.Grab.OnPressed += HandleGrabPressed;
            _interactorInputContainer.ScrollTickUp.OnTickOver += HandleScrollUp;
            _interactorInputContainer.ScrollTickDown.OnTickOver += HandleScrollDown;

            if (_WaitingForMultiplayerSupport)
                _multiplayerSupport.OnConnectedToInstance += RegisterWithContainer;
            else
                RegisterWithContainer();
        }

        public virtual void HandleOnDisable()
        {
            Debug.Log("Random ID: " + _randomIDTEMPDEBUG + " unsubscribed from input");

            _interactorInputContainer.RangedClick.OnPressed -= HandleRangedClickPressed;
            _interactorInputContainer.HandheldClick.OnPressed -= HandleHandheldClickPressed;
            _interactorInputContainer.Grab.OnPressed -= HandleGrabPressed;
            _interactorInputContainer.ScrollTickUp.OnTickOver -= HandleScrollUp;
            _interactorInputContainer.ScrollTickDown.OnTickOver -= HandleScrollDown;

            if (_multiplayerSupport != null)
                _multiplayerSupport.OnConnectedToInstance -= RegisterWithContainer;

            _interactorContainer.DeregisterInteractor(_InteractorID.ToString());
        }

        private void RegisterWithContainer() 
        {
            Debug.Log("Added to container");
            if (_multiplayerSupport != null)
                _multiplayerSupport.OnConnectedToInstance -= RegisterWithContainer;

            _interactorContainer.RegisterInteractor(_InteractorID.ToString(), this);
        }

        public void HandleUpdate()
        {
            if (IsCurrentlyGrabbing)
                return;

            RaycastResultWrapper raycastResultWrapper = GetRayCastResult();

            if (!_WaitingForMultiplayerSupport && raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange)
            {
                bool isAllowedToInteract = !raycastResultWrapper.RangedInteractable.AdminOnly;
                SetInteractorState(isAllowedToInteract ? InteractorState.InteractionAvailable : InteractorState.InteractionLocked);
                _raycastHitDebug.Value = raycastResultWrapper.RangedInteractable.ToString();
            }
            else
            {
                SetInteractorState(InteractorState.Idle);
                _raycastHitDebug.Value = "none";
            }

            HandleRaycastDistance(raycastResultWrapper.HitDistance);
        }

        protected virtual void HandleRaycastDistance(float distance) { } //TODO: Code smell? InteractorVR needs this to set the LineRenderer length

        private RaycastResultWrapper GetRayCastResult() =>
            _RaycastProvider.Raycast(_RayOrigin.position, _RayOrigin.forward, MAX_RAYCAST_DISTANCE,  _layerMask);
        
        private void HandleRangedClickPressed()
        {
            if (_WaitingForMultiplayerSupport || IsCurrentlyGrabbing)
                return;

            RaycastResultWrapper raycastResultWrapper = GetRayCastResult();

            if (raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange &&
                raycastResultWrapper.RangedInteractable is IRangedClickInteractionModule rangedClickInteractable)
            {
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

