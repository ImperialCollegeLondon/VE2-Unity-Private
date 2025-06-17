using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.Player.API;
using VE2.Core.UI.API;
using VE2.Core.VComponents.API;

namespace VE2.Core.Player.Internal
{
    internal class InteractorReferences
    {
        public Transform InteractorParentTransform => _interactorParentTransform;
        [SerializeField, IgnoreParent] private Transform _interactorParentTransform;

        public Transform GrabberVisualisationRayOrigin => _grabberVisualisationRayOrigin;
        [SerializeField, IgnoreParent] private Transform _grabberVisualisationRayOrigin; 

        public Transform GrabberTransform => _grabberTransform;
        [SerializeField, IgnoreParent] private Transform _grabberTransform;

        public GameObject GrabberVisualisation => _grabberVisualisation;
        [SerializeField, IgnoreParent] private GameObject _grabberVisualisation;

        public Transform RayOrigin => _rayOrigin;
        [SerializeField, IgnoreParent] private Transform _rayOrigin;

        [SerializeField] private StringWrapper _raycastHitDebug;
        public StringWrapper RaycastHitDebug => _raycastHitDebug;
    }

    [Serializable]
    internal class StringWrapper
    {
        [SerializeField, IgnoreParent] public string Value;
    }

    [Serializable]
    internal class FreeGrabbableWrapper
    {
        public IRangedFreeGrabInteractionModule RangedFreeGrabInteraction { get; internal set; }
    }

    internal abstract class PointerInteractor : IInteractor
    {
        public ITransformWrapper GrabberTransformWrapper { get; }
        public IReadOnlyList<string> HeldNetworkedActivatableIDs => _heldActivatableIDsAgainstNetworkFlags.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();

        protected bool IsCurrentlyGrabbing => _CurrentGrabbingGrabbable != null;
        protected InteractorID _InteractorID => _LocalClientIDWrapper.IsClientIDReady ? new InteractorID(_LocalClientIDWrapper.Value, _InteractorType) : null;
        protected readonly Dictionary<string, bool> _heldActivatableIDsAgainstNetworkFlags = new();
        protected const float MAX_RAYCAST_DISTANCE = 30;
        protected const float MAX_SPHERECAST_RADIUS = 10;
        protected IRangedInteractionModule _CurrentHoveringInteractable;
        protected IRangedClickInteractionModule _CurrentHoveringClickInteractable => _CurrentHoveringInteractable as IRangedClickInteractionModule;


        protected IRangedGrabInteractionModule _CurrentGrabbingGrabbable;


        private GameObject lastHoveredUIObject = null; // Keep track of the last hovered UI object

        private readonly HandInteractorContainer _interactorContainer;
        private readonly IGrabInteractablesContainer _grabInteractablesContainer;
        private readonly InteractorInputContainer _interactorInputContainer;

        private readonly LayerMask _raycastLayerMask;

        protected readonly Transform _interactorParentTransform;
        protected readonly Transform _GrabberTransform;
        private readonly LineRenderer _grabbableLineVisLineRenderer;
        protected readonly GameObject _GrabberVisualisation;
        protected readonly Transform _GrabberVisualisationRayOrigin;

        protected readonly Transform _RayOrigin;
        private readonly StringWrapper _raycastHitDebug;

        private readonly InteractorType _InteractorType;
        private readonly IRaycastProvider _RaycastProvider;
        protected readonly ILocalClientIDWrapper _LocalClientIDWrapper;
        protected readonly ILocalAdminIndicator _localAdminIndicator;

        protected const float LOW_HAPTICS_AMPLITUDE = 0.2f;
        protected const float HIGH_HAPTICS_AMPLITUDE = 0.5f;
        protected const float HIGH_HAPTICS_DURATION = 0.1f;
        protected const float LOW_HAPTICS_DURATION = 0.05f;

        //TODO - this can probably live just in InteractorVR... is there any reason the 2d interactor needs this? Think its just for teleporting?
        internal readonly FreeGrabbableWrapper GrabbableWrapper;
        private readonly HoveringOverScrollableIndicator _hoveringOverScrollableIndicator;

        //TODO - should probably be injecting transform wrappers here rather than raw transforms
        internal PointerInteractor(HandInteractorContainer interactorContainer, IGrabInteractablesContainer grabInteractablesContainer, InteractorInputContainer interactorInputContainer, PlayerInteractionConfig interactionConfig,
            InteractorReferences interactorReferences, InteractorType interactorType, IRaycastProvider raycastProvider,
            ILocalClientIDWrapper localClientIDWrapper, ILocalAdminIndicator localAdminIndicator, FreeGrabbableWrapper grabbableWrapper, HoveringOverScrollableIndicator hoveringOverScrollableIndicator)
        {
            _interactorContainer = interactorContainer;
            _grabInteractablesContainer = grabInteractablesContainer;
            _interactorInputContainer = interactorInputContainer;
            _raycastLayerMask = interactionConfig.InteractableLayers;

            _interactorParentTransform = interactorReferences.InteractorParentTransform;
            _GrabberTransform = interactorReferences.GrabberTransform;
            GrabberTransformWrapper = new TransformWrapper(_GrabberTransform);
            _GrabberVisualisation = interactorReferences.GrabberVisualisation;
            _GrabberVisualisationRayOrigin = interactorReferences.GrabberVisualisationRayOrigin;
            _grabbableLineVisLineRenderer = _GrabberVisualisation.GetComponent<LineRenderer>();
            _RayOrigin = interactorReferences.RayOrigin;
            _raycastHitDebug = interactorReferences.RaycastHitDebug;

            _InteractorType = interactorType;
            _RaycastProvider = raycastProvider;

            _LocalClientIDWrapper = localClientIDWrapper;
            _localAdminIndicator = localAdminIndicator;

            GrabbableWrapper = grabbableWrapper;

            _hoveringOverScrollableIndicator = hoveringOverScrollableIndicator;
        }

        public virtual void HandleOnEnable()
        {
            _interactorInputContainer.RangedClick.OnPressed += HandleRangedClickPressed;
            _interactorInputContainer.RangedClick.OnReleased += HandleRangedClickReleased;
            _interactorInputContainer.HandheldClick.OnPressed += HandleHandheldClickPressed;
            _interactorInputContainer.HandheldClick.OnReleased += HandleHandheldClickReleased;
            _interactorInputContainer.Grab.OnPressed += HandleGrabPressed;
            _interactorInputContainer.ScrollTickUp.OnTickOver += HandleScrollUp;
            _interactorInputContainer.ScrollTickDown.OnTickOver += HandleScrollDown;

            _heldActivatableIDsAgainstNetworkFlags.Clear();

            if (!_LocalClientIDWrapper.IsClientIDReady)
                _LocalClientIDWrapper.OnClientIDReady += HandleLocalClientIDReady;
            else
                HandleLocalClientIDReady(_LocalClientIDWrapper.Value);
        }

        public virtual void HandleOnDisable()
        {
            _interactorInputContainer.RangedClick.OnPressed -= HandleRangedClickPressed;
            _interactorInputContainer.RangedClick.OnReleased -= HandleRangedClickReleased;
            _interactorInputContainer.HandheldClick.OnPressed -= HandleHandheldClickPressed;
            _interactorInputContainer.HandheldClick.OnReleased -= HandleHandheldClickReleased;
            _interactorInputContainer.Grab.OnPressed -= HandleGrabPressed;
            _interactorInputContainer.ScrollTickUp.OnTickOver -= HandleScrollUp;
            _interactorInputContainer.ScrollTickDown.OnTickOver -= HandleScrollDown;

            _heldActivatableIDsAgainstNetworkFlags.Clear();

            _LocalClientIDWrapper.OnClientIDReady -= HandleLocalClientIDReady;
            _interactorContainer?.DeregisterInteractor(_InteractorID.ToString());
        }

        // Only allow interactable if not admin only, or if local player is admin
        protected bool IsInteractableAllowed(IGeneralInteractionModule interactable)
        {
            return interactable != null && (!interactable.AdminOnly || _localAdminIndicator.IsLocalAdmin);
        }

        protected abstract void Vibrate(float amplitude, float duration);

        protected virtual void HandleLocalClientIDReady(ushort clientID)
        {
            _LocalClientIDWrapper.OnClientIDReady -= HandleLocalClientIDReady;
            _interactorContainer.RegisterInteractor(_InteractorID.ToString(), this);
        }

        public virtual void HandleUpdate()
        {
            RaycastResultWrapper raycastResultWrapper = GetRayCastResult();
            RaycastResultWrapper sphereCastResultWrapper = GetSphereCastResult(); // for 2D interactor, this will be null

            IRangedInteractionModule previousHoveringInteractable = _CurrentHoveringInteractable;

            //Update the current hovering interactable, as long as we're not waiting for id, and it's not a grabbable that we were previously hovering over
            if (_LocalClientIDWrapper.IsClientIDReady && !(previousHoveringInteractable is IRangedGrabInteractionModule previousRangedGrabInteractable && _CurrentGrabbingGrabbable == previousRangedGrabInteractable))
            {
                if (raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange && IsInteractableAllowed(raycastResultWrapper.RangedInteractableInRange))
                    _CurrentHoveringInteractable = raycastResultWrapper.RangedInteractableInRange;
                else if(this is InteractorVR && !raycastResultWrapper.HitInteractable && sphereCastResultWrapper != null && sphereCastResultWrapper.HitInteractable && sphereCastResultWrapper.RangedInteractableIsInRange)
                    _CurrentHoveringInteractable = sphereCastResultWrapper.RangedInteractableInRange;
                else
                    _CurrentHoveringInteractable = null;
            }

            //If we've stopped hovering over something, call exit hover. If we were holding its click down, release
            if (previousHoveringInteractable != null && previousHoveringInteractable != _CurrentHoveringInteractable)
            {
                previousHoveringInteractable.ExitHover(_InteractorID);

                if (previousHoveringInteractable is IRangedHoldClickInteractionModule previousHoldClickInteractable && _heldActivatableIDsAgainstNetworkFlags.ContainsKey(previousHoldClickInteractable.ID))
                {
                    Vibrate(HIGH_HAPTICS_AMPLITUDE, HIGH_HAPTICS_DURATION);
                    previousHoldClickInteractable.ClickUp(_InteractorID);
                    _heldActivatableIDsAgainstNetworkFlags.Remove(previousHoldClickInteractable.ID);
                }
            }

            //If we've started hovering over something, call enter hover
            if (_LocalClientIDWrapper.IsClientIDReady && _CurrentHoveringInteractable != null && _CurrentHoveringInteractable != previousHoveringInteractable)
            {
                if (_CurrentHoveringClickInteractable != null && this is InteractorVR && !_CurrentHoveringClickInteractable.ActivateAtRangeInVR)
                    return;

                _CurrentHoveringInteractable.EnterHover(_InteractorID);
                Vibrate(LOW_HAPTICS_AMPLITUDE, LOW_HAPTICS_DURATION);
            }

            //if grabbing an adjustable module, update the visualisation, and update input value
            if (IsCurrentlyGrabbing && _CurrentGrabbingGrabbable is IRangedAdjustableInteractionModule rangedAdjustableInteraction)
            {
                _hoveringOverScrollableIndicator.IsHoveringOverScrollableObject = false;
                _grabbableLineVisLineRenderer.startWidth = _grabbableLineVisLineRenderer.endWidth = 0.005f;
                _grabbableLineVisLineRenderer.SetPosition(0, _GrabberVisualisationRayOrigin.position);
                _grabbableLineVisLineRenderer.SetPosition(1, rangedAdjustableInteraction.Transform.position);

                HandleUpdateGrabbingAdjustable();
            }
            else if (!IsCurrentlyGrabbing)
            {
                bool isAllowedToInteract = false;

                //If hovering over an interactable, handle interactor and hover=========
                if (_LocalClientIDWrapper.IsClientIDReady && (raycastResultWrapper.HitUIButton || raycastResultWrapper.HitInteractableInRange))
                {
                    if (raycastResultWrapper.HitInteractable)
                    {
                        isAllowedToInteract = !raycastResultWrapper.RangedInteractable.AdminOnly;
                        if (raycastResultWrapper.RangedInteractable is IRangedClickInteractionModule rangedClickInteraction && this is InteractorVR)
                            isAllowedToInteract &= rangedClickInteraction.ActivateAtRangeInVR;

                        _hoveringOverScrollableIndicator.IsHoveringOverScrollableObject = raycastResultWrapper.HitScrollableInteractableInRange;
                        _raycastHitDebug.Value = raycastResultWrapper.RangedInteractable.ToString();
                    }
                    else if (raycastResultWrapper.HitUIButton)
                    {
                        isAllowedToInteract = raycastResultWrapper.UIButton.interactable;
                        _hoveringOverScrollableIndicator.IsHoveringOverScrollableObject = raycastResultWrapper.HitScrollableUI;
                        _raycastHitDebug.Value = raycastResultWrapper.HitInteractable ? raycastResultWrapper.RangedInteractable.ToString() : raycastResultWrapper.UIButton.name;

                        if (isAllowedToInteract)
                            HandleHoverOverUIGameObject(raycastResultWrapper.UIButton.gameObject);
                        else
                            HandleNoHoverOverUIGameObject();
                    }

                    SetInteractorState(isAllowedToInteract ? InteractorState.InteractionAvailable : InteractorState.InteractionLocked);
                }
                else
                {
                    //check if were in vr and spherecast actually hits something
                    if (this is InteractorVR && !raycastResultWrapper.HitInteractable)
                    {
                        if (sphereCastResultWrapper != null && sphereCastResultWrapper.HitInteractable && sphereCastResultWrapper.RangedInteractableIsInRange)
                        {
                            isAllowedToInteract = !sphereCastResultWrapper.RangedInteractable.AdminOnly;

                            _hoveringOverScrollableIndicator.IsHoveringOverScrollableObject = sphereCastResultWrapper.HitScrollableInteractableInRange;
                            _raycastHitDebug.Value = sphereCastResultWrapper.RangedInteractable.ToString();

                            SetInteractorState(isAllowedToInteract ? InteractorState.InteractionAvailable : InteractorState.InteractionLocked);

                        }
                        else
                        {
                            //set this here because otherwise it doesnt return back to idle state
                            _hoveringOverScrollableIndicator.IsHoveringOverScrollableObject = false;
                            HandleNoHoverOverUIGameObject();
                            SetInteractorState(InteractorState.Idle);
                            _raycastHitDebug.Value = "none";
                        }

                    }
                    else
                    {
                        _hoveringOverScrollableIndicator.IsHoveringOverScrollableObject = false;
                        HandleNoHoverOverUIGameObject();
                        SetInteractorState(InteractorState.Idle);
                        _raycastHitDebug.Value = "none";
                    }
                }
                
                //break out of the whole thing if we're in 2D, this bit is just to manage the raycast distance for the VR interactor
                if(this is Interactor2D)
                    return;

                //If the main ray hits an interactable, point the line at that
                //Otherwise, if the proximity ray hits an interactable, point the line at that
                //OTHERWISE, if the main ray hit ANYTHING, point the line at that
                //If none of those, just point the ray forward at max distance 
                Vector3 rayEndPosition;
                if (raycastResultWrapper.HitInteractableOrUI)
                    rayEndPosition = raycastResultWrapper.HitPosition;
                else if (sphereCastResultWrapper.HitInteractableOrUI)
                    rayEndPosition = sphereCastResultWrapper.HitPosition;
                else if (raycastResultWrapper.HitAnything)
                    rayEndPosition = raycastResultWrapper.HitPosition;
                else 
                    rayEndPosition = _RayOrigin.position + _RayOrigin.forward * MAX_RAYCAST_DISTANCE;
                
                //jank way to set the parameters for the raycast distance
                HandleRaycastDistance(rayEndPosition);
            }
        }

        protected abstract void HandleUpdateGrabbingAdjustable();

        private void HandleHoverOverUIGameObject(GameObject go)
        {
            if (go == lastHoveredUIObject)
                return;

            if (lastHoveredUIObject != null)
                InformUIObjectEndHover(lastHoveredUIObject);

            lastHoveredUIObject = go;
            InformUIObjectStartHover(go);
        }

        private void HandleNoHoverOverUIGameObject()
        {
            if (lastHoveredUIObject == null)
                return;

            InformUIObjectEndHover(lastHoveredUIObject);
            lastHoveredUIObject = null;
        }

        private void InformUIObjectStartHover(GameObject go)
        {
            ExecuteEvents.Execute<IPointerEnterHandler>(
                go,
                new PointerEventData(EventSystem.current),
                (handler, eventData) => handler.OnPointerEnter((PointerEventData)eventData)
            );

            if (go.TryGetComponent(out IUIColorHandler colorHandler))
                colorHandler.OnPointerEnter();
        }

        private void InformUIObjectEndHover(GameObject go)
        {
            ExecuteEvents.Execute<IPointerExitHandler>(
                go,
                new PointerEventData(EventSystem.current),
                (handler, eventData) => handler.OnPointerExit((PointerEventData)eventData)
            );

            if (go.TryGetComponent(out IUIColorHandler colorHandler))
                colorHandler.OnPointerExit();
        }

        protected virtual void HandleRaycastDistance(Vector3 point) { } //TODO: Code smell? InteractorVR needs this to set the LineRenderer length

        protected RaycastResultWrapper GetRayCastResult()
        {
            if (_RayOrigin == null)
                return null;

            return _RaycastProvider.Raycast(_RayOrigin.position, _RayOrigin.forward, MAX_RAYCAST_DISTANCE, _raycastLayerMask);
        }

        private RaycastResultWrapper GetSphereCastResult(bool failsafeGrab = false)
        {
            if (_GrabberTransform == null || this is Interactor2D)
                return null;

            if (_InteractorType == InteractorType.LeftHandVR)
                return _RaycastProvider.SphereCastAll(_GrabberTransform.position, MAX_SPHERECAST_RADIUS, _GrabberTransform.up, 0f, _raycastLayerMask, failsafeGrab, _GrabberTransform.right);
            if (_InteractorType == InteractorType.RightHandVR)
                return _RaycastProvider.SphereCastAll(_GrabberTransform.position, MAX_SPHERECAST_RADIUS, _GrabberTransform.up, 0f, _raycastLayerMask, failsafeGrab, -_GrabberTransform.right);
            else
                return _RaycastProvider.SphereCastAll(_GrabberTransform.position, MAX_SPHERECAST_RADIUS, _GrabberTransform.up, 0f, _raycastLayerMask);
        }

        private void HandleRangedClickPressed()
        {
            if (!_LocalClientIDWrapper.IsClientIDReady || IsCurrentlyGrabbing)
                return;

            RaycastResultWrapper raycastResultWrapper = GetRayCastResult();

            if (raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange &&
                raycastResultWrapper.RangedInteractable is IRangedClickInteractionModule rangedClickInteractable && IsInteractableAllowed(rangedClickInteractable))
            {
                //TODO - Code smell? This is a bit of a hack to get around the fact that we don't have a way to check if we're in VR or not
                if (this is InteractorVR && !rangedClickInteractable.ActivateAtRangeInVR)
                    return;

                rangedClickInteractable.ClickDown(_InteractorID);
                Vibrate(HIGH_HAPTICS_AMPLITUDE, HIGH_HAPTICS_DURATION);
                _CurrentHoveringInteractable = rangedClickInteractable;

                if (rangedClickInteractable is IRangedHoldClickInteractionModule rangedHoldClickInteractable)
                    _heldActivatableIDsAgainstNetworkFlags.Add(rangedClickInteractable.ID, rangedHoldClickInteractable.IsNetworked);
            }
            else if (raycastResultWrapper.HitUIButton && raycastResultWrapper.UIButton.IsInteractable())
            {
                raycastResultWrapper.UIButton.onClick.Invoke();
                Vibrate(HIGH_HAPTICS_AMPLITUDE, HIGH_HAPTICS_DURATION); 
            }
        }

        private void HandleRangedClickReleased()
        {
            if (!_LocalClientIDWrapper.IsClientIDReady || IsCurrentlyGrabbing)
                return;

            if (_CurrentHoveringClickInteractable != null && _CurrentHoveringClickInteractable is IRangedHoldClickInteractionModule _CurrentHoveringHoldClickInteractable ) //WHAT HAPPENS WHEN YOU'RE MID HOVERING AND ADMIN ONLY COMES ON??
            {
                _CurrentHoveringHoldClickInteractable.ClickUp(_InteractorID);
                _heldActivatableIDsAgainstNetworkFlags.Remove(_CurrentHoveringHoldClickInteractable.ID);
                Vibrate(HIGH_HAPTICS_AMPLITUDE, HIGH_HAPTICS_DURATION);
            }
        }

        private void HandleGrabPressed()
        {
            if (IsCurrentlyGrabbing)
            {
                CheckForExitInspectMode();
                IRangedGrabInteractionModule rangedGrabInteractableToDrop = _CurrentGrabbingGrabbable;
                rangedGrabInteractableToDrop.RequestLocalDrop(_InteractorID);
                Vibrate(HIGH_HAPTICS_AMPLITUDE, HIGH_HAPTICS_DURATION);
            }
            else
            {
                RaycastResultWrapper raycastResultWrapper = GetRayCastResult();

                if (_LocalClientIDWrapper.IsClientIDReady)
                {
                    if (raycastResultWrapper != null && raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange)
                    {
                        if (IsInteractableAllowed(raycastResultWrapper.RangedInteractable))
                        {
                            if (raycastResultWrapper.RangedInteractable is IRangedGrabInteractionModule rangedGrabInteractable)
                            {
                                rangedGrabInteractable.RequestLocalGrab(_InteractorID);
                                Vibrate(HIGH_HAPTICS_AMPLITUDE, HIGH_HAPTICS_DURATION);
                            }
                        }
                        else
                        {
                            //TODO, maybe play an error sound or something
                        }
                    }
                    else
                    {
                        if (this is Interactor2D)
                            return;

                        RaycastResultWrapper sphereCastResultWrapper = GetSphereCastResult(failsafeGrab: true);

                        if (sphereCastResultWrapper != null && sphereCastResultWrapper.HitInteractable && sphereCastResultWrapper.RangedInteractableIsInRange)
                        {
                            if (IsInteractableAllowed(sphereCastResultWrapper.RangedInteractable))
                            {
                                if (sphereCastResultWrapper.RangedInteractable is IRangedGrabInteractionModule rangedGrabInteractable)
                                {
                                    rangedGrabInteractable.RequestLocalGrab(_InteractorID);
                                    Vibrate(HIGH_HAPTICS_AMPLITUDE, HIGH_HAPTICS_DURATION);
                                }
                            }
                            else
                            {
                                //TODO, maybe play an error sound or something
                            }
                        }
                    }
                }
            }
        }

        protected virtual void CheckForExitInspectMode() { } //Do nothing, unless overridden by 2d interactor

        public void ConfirmGrab(string id)

        {
            if (!_grabInteractablesContainer.GrabInteractables.TryGetValue(id, out IRangedGrabInteractionModule rangedGrabInteractable))
            {
                Debug.LogError($"Failed to confirm grab, could not find grabbable with id {id}");
                return;
            }

            _CurrentGrabbingGrabbable = rangedGrabInteractable;

            SetInteractorState(InteractorState.Grabbing);

            if (rangedGrabInteractable is IRangedFreeGrabInteractionModule rangedFreeGrabInteractable && GrabbableWrapper != null)
            {
                GrabbableWrapper.RangedFreeGrabInteraction = rangedFreeGrabInteractable;
            }
            else if (_CurrentGrabbingGrabbable is IRangedAdjustableInteractionModule rangedAdjustableInteraction)
            {
                HandleStartGrabbingAdjustable(rangedAdjustableInteraction);
                _GrabberVisualisation.SetActive(true);
            }
        }

        protected abstract void HandleStartGrabbingAdjustable(IRangedAdjustableInteractionModule rangedAdjustableInteraction);

        public void ConfirmDrop()
        {
            SetInteractorState(InteractorState.Idle);

            if (_CurrentGrabbingGrabbable is IRangedAdjustableInteractionModule)
                HandleStopGrabbingAdjustable();

            _CurrentGrabbingGrabbable = null;

            //reset the virtual grabber transform to the original position
            _GrabberTransform.localPosition = Vector3.zero;
            _GrabberVisualisation.SetActive(false);

            if (GrabbableWrapper != null)
                GrabbableWrapper.RangedFreeGrabInteraction = null;
        }

        protected abstract void HandleStopGrabbingAdjustable();

        private void HandleHandheldClickPressed()
        {
            if (_LocalClientIDWrapper.IsClientIDReady && IsCurrentlyGrabbing)
            {
                foreach (IHandheldInteractionModule handheldInteraction in _CurrentGrabbingGrabbable.HandheldInteractions)
                {
                    if (handheldInteraction is IHandheldClickInteractionModule handheldClickInteraction)
                    {
                        handheldClickInteraction.ClickDown(_InteractorID.ClientID);
                        Vibrate(HIGH_HAPTICS_AMPLITUDE, HIGH_HAPTICS_DURATION);
                    }
                }
            }
        }

        private void HandleHandheldClickReleased()
        {
            if (_InteractorID != null && IsCurrentlyGrabbing)
            {
                foreach (IHandheldInteractionModule handheldInteraction in _CurrentGrabbingGrabbable.HandheldInteractions)
                {
                    if (handheldInteraction is IHandheldClickInteractionModule handheldClickInteraction)
                    {
                        handheldClickInteraction.ClickUp(_InteractorID.ClientID);
                        Vibrate(HIGH_HAPTICS_AMPLITUDE, HIGH_HAPTICS_DURATION);
                    }
                }
            }
        }
        protected virtual void CheckForInspectModeOnScroll(bool scrollUp) { }
        private void HandleScrollUp()
        {
            if (IsCurrentlyGrabbing)
            {
                CheckForInspectModeOnScroll(true);
                foreach (IHandheldInteractionModule handheldInteraction in _CurrentGrabbingGrabbable.HandheldInteractions)
                {
                    if (handheldInteraction is IHandheldScrollInteractionModule handheldScrollInteraction)
                    {
                        handheldScrollInteraction.ScrollUp(_InteractorID.ClientID);
                        Vibrate(HIGH_HAPTICS_AMPLITUDE, HIGH_HAPTICS_DURATION);
                    }
                }
            }
            else
            {
                RaycastResultWrapper raycastResultWrapper = GetRayCastResult();

                if (_LocalClientIDWrapper.IsClientIDReady && raycastResultWrapper != null && raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange)
                {
                    if (IsInteractableAllowed(raycastResultWrapper.RangedInteractable))
                    {
                        //if while scrolling up, raycast returns an adjustable module
                        if (raycastResultWrapper.RangedInteractable is IRangedAdjustableInteractionModule rangedAdjustableInteraction)
                        {
                            rangedAdjustableInteraction.ScrollUp();
                            Vibrate(HIGH_HAPTICS_AMPLITUDE, HIGH_HAPTICS_DURATION);
                        }
                    }
                }
            }
        }

        private void HandleScrollDown()
        {
            if (IsCurrentlyGrabbing)
            {
                CheckForInspectModeOnScroll(false);
                foreach (IHandheldInteractionModule handheldInteraction in _CurrentGrabbingGrabbable.HandheldInteractions)
                {
                    if (handheldInteraction is IHandheldScrollInteractionModule handheldScrollInteraction)
                    {
                        handheldScrollInteraction.ScrollDown(_InteractorID.ClientID);
                        Vibrate(HIGH_HAPTICS_AMPLITUDE, HIGH_HAPTICS_DURATION);
                    }
                }
            }
            else
            {
                RaycastResultWrapper raycastResultWrapper = GetRayCastResult();

                if (_LocalClientIDWrapper.IsClientIDReady && raycastResultWrapper != null && raycastResultWrapper.HitInteractable && raycastResultWrapper.RangedInteractableIsInRange)
                {
                    if (IsInteractableAllowed(raycastResultWrapper.RangedInteractable))
                    {
                        //if while scrolling up, raycast returns an adjustable module
                        if (raycastResultWrapper.RangedInteractable is IRangedAdjustableInteractionModule rangedAdjustableInteraction)
                        {
                            rangedAdjustableInteraction.ScrollDown();
                            Vibrate(HIGH_HAPTICS_AMPLITUDE, HIGH_HAPTICS_DURATION);
                        }
                    }
                }
            }
        }

        protected abstract void SetInteractorState(InteractorState newState);

        internal enum InteractorState

        {
            Idle,
            InteractionAvailable,
            InteractionLocked,
            Grabbing
        }
    }
}

