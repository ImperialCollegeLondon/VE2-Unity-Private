using System.Collections.Generic;
using UnityEngine;
using VE2.Core.Player.API;
using VE2.Core.UI.API;
using VE2.Core.VComponents.API;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.Internal
{
    internal class PlayerControllerVR : BasePlayerController
    {
        public PlayerTransformData PlayerTransformData
        {
            get
            {
                return new PlayerTransformData(
                    IsVRMode: true,
                    rootPosition: _rootTransform.position,
                    rootRotation: _rootTransform.rotation,
                    verticalOffset: _verticalOffsetTransform.localPosition.y,
                    headPosition: _headTransform.transform.localPosition,
                    headRotation: _headTransform.transform.localRotation,
                    handVRLeftPosition: _handControllerLeft.Transform.localPosition,
                    handVRLeftRotation: _handControllerLeft.Transform.localRotation,
                    handVRRightPosition: _handControllerRight.Transform.localPosition,
                    handVRRightRotation: _handControllerRight.Transform.localRotation,
                    activatableIDsVRLeft: _handControllerLeft.HeldActivatableIDs,
                    activatableIDsVRRight: _handControllerRight.HeldActivatableIDs,
                    activatableIDsFeet: _feetInteractorVR.HeldActivatableIDs
                    
                );
            }
        }

        private readonly GameObject _playerGO;
        private readonly PlayerVRInputContainer _playerVRInputContainer;
        private readonly PlayerVRControlConfig _controlConfig;
        private readonly IXRManagerWrapper _xrManagerSettingsWrapper;
        private readonly Transform _rootTransform;
        private readonly Transform _verticalOffsetTransform;
        private readonly Transform _headTransform;
        private readonly FeetInteractor _feetInteractorVR;

        private readonly V_HandController _handControllerLeft;
        private readonly V_HandController _handControllerRight;

        //TODO - it's both players that need these. 
        //That suggests we probably want this in a base class? Think about refactoring
        private readonly IPrimaryUIServiceInternal _primaryUIService; //Secondary lives on the hands
        private readonly RectTransform _primaryUIHolderRect;
        private readonly ISecondaryUIServiceInternal _secondaryUIService;

        internal PlayerControllerVR(HandInteractorContainer interactorContainer, PlayerVRInputContainer playerVRInputContainer, IPlayerPersistentDataHandler playerSettingsHandler, PlayerVRControlConfig controlConfig, MovementModeConfig movementModeConfig,
            IRaycastProvider raycastProvider, IXRManagerWrapper xrManagerSettingsWrapper, ILocalClientIDProvider localClientIDProvider, IPrimaryUIServiceInternal primaryUIService, ISecondaryUIServiceInternal secondaryUIService) 
        {
            GameObject playerVRPrefab = Resources.Load("vrPlayer") as GameObject;
            _playerGO = GameObject.Instantiate(playerVRPrefab, null, false);
            _playerGO.SetActive(false);

            _playerVRInputContainer = playerVRInputContainer;
            _controlConfig = controlConfig;
            _xrManagerSettingsWrapper = xrManagerSettingsWrapper;

            _primaryUIService = primaryUIService;
            _secondaryUIService = secondaryUIService;

            PlayerVRReferences playerVRReferences = _playerGO.GetComponent<PlayerVRReferences>();
            _rootTransform = playerVRReferences.RootTransform;
            _verticalOffsetTransform = playerVRReferences.VerticalOffsetTransform;
            _headTransform = playerVRReferences.HeadTransform;
            _primaryUIHolderRect = playerVRReferences.PrimaryUIHolderRect;

            base._PlayerHeadTransform = _headTransform;
            base._FeetCollisionDetector = playerVRReferences.FeetCollisionDetector;
    
            _feetInteractorVR = new FeetInteractor(_FeetCollisionDetector, InteractorType.Feet, localClientIDProvider);

            GameObject handVRLeftPrefab = Resources.Load<GameObject>("HandVRLeft");
            GameObject handVRLeftGO = GameObject.Instantiate(handVRLeftPrefab, _verticalOffsetTransform, false);
            GameObject handVRRightGO = GameObject.Instantiate(handVRLeftPrefab, _verticalOffsetTransform, false);
            handVRRightGO.transform.localScale = new Vector3(-1, 1, 1);
            handVRRightGO.name = "HandVRRight";

            FreeGrabbableWrapper leftHandGrabbableWrapper = new FreeGrabbableWrapper();
            FreeGrabbableWrapper rightHandGrabbableWrapper = new FreeGrabbableWrapper();

            _handControllerLeft = CreateHandController(handVRLeftGO, handVRRightGO, interactorContainer, 
                playerVRInputContainer.HandVRLeftInputContainer, playerVRInputContainer.HandVRRightInputContainer.DragLocomotorInputContainer, 
                InteractorType.LeftHandVR, raycastProvider, localClientIDProvider, leftHandGrabbableWrapper, rightHandGrabbableWrapper, secondaryUIService, movementModeConfig, false);
            
            _handControllerRight = CreateHandController(handVRRightGO, handVRLeftGO, interactorContainer, 
                playerVRInputContainer.HandVRRightInputContainer,playerVRInputContainer.HandVRLeftInputContainer.DragLocomotorInputContainer, 
                InteractorType.RightHandVR, raycastProvider, localClientIDProvider, rightHandGrabbableWrapper, leftHandGrabbableWrapper, secondaryUIService, movementModeConfig, true);
        }

        
        private V_HandController CreateHandController(GameObject handGO, GameObject otherHandGO, HandInteractorContainer interactorContainer, 
            HandVRInputContainer handVRInputContainer, DragLocomotorInputContainer otherHandDragInputContainer, InteractorType interactorType, 
            IRaycastProvider raycastProvider, ILocalClientIDProvider multiplayerSupport, FreeGrabbableWrapper thisHandGrabbableWrapper, 
            FreeGrabbableWrapper otherHandGrabbableWrapper, ISecondaryUIServiceInternal secondaryUIService, MovementModeConfig movementModeConfig, bool needsToFlip)
        {
            V_HandVRReferences thisHandVRReferences = handGO.GetComponent<V_HandVRReferences>();
            V_HandVRReferences otherHandVRReferences = otherHandGO.GetComponent<V_HandVRReferences>();

            HoveringOverScrollableIndicator hoveringOverScrollableIndicator = new();

            InteractorVR interactor = new(
                interactorContainer, handVRInputContainer.InteractorVRInputContainer,
                thisHandVRReferences.InteractorVRReferences,
                interactorType, raycastProvider, multiplayerSupport, thisHandGrabbableWrapper, hoveringOverScrollableIndicator);

            DragLocomotorController dragLocomotor = new(
                thisHandVRReferences.LocomotorVRReferences,
                handVRInputContainer.DragLocomotorInputContainer, otherHandDragInputContainer,
                _rootTransform, _verticalOffsetTransform, _headTransform, handGO.transform,
                movementModeConfig);

            SnapTurnController snapTurn = new(
                handVRInputContainer.SnapTurnInputContainer,
                _rootTransform,
                handVRInputContainer.TeleportInputContainer, thisHandGrabbableWrapper,otherHandGrabbableWrapper, thisHandVRReferences.InteractorVRReferences.RayOrigin, otherHandVRReferences.InteractorVRReferences.RayOrigin);

            TeleportController teleport = new(
                handVRInputContainer.TeleportInputContainer,
                thisHandVRReferences.TeleporterReferences.TeleportLineRenderer, thisHandVRReferences.InteractorVRReferences.LineRenderer, thisHandVRReferences.TeleporterReferences.TeleportCursorPrefab,
                _rootTransform, _headTransform, otherHandVRReferences.InteractorVRReferences.RayOrigin,
                thisHandGrabbableWrapper, otherHandGrabbableWrapper, hoveringOverScrollableIndicator, movementModeConfig);

            WristUIHandler wristUIHandler = new(
                secondaryUIService, thisHandVRReferences.WristUIReferences.WristUIHolder, _headTransform, thisHandVRReferences.WristUIReferences.Indicator, needsToFlip);
            
            return new V_HandController(handGO, handVRInputContainer, interactor, dragLocomotor, snapTurn, teleport, wristUIHandler);
        }

        public void ActivatePlayer(PlayerTransformData initTransformData)
        {
            _playerGO.SetActive(true);

            _rootTransform.SetPositionAndRotation(initTransformData.RootPosition, initTransformData.RootRotation);
            _verticalOffsetTransform.localPosition = new Vector3(0, initTransformData.VerticalOffset, 0);
            //We don't set head transform here, tracking will override it anyway

            if (_xrManagerSettingsWrapper.IsInitializationComplete)
                HandleXRInitComplete();
            else
                _xrManagerSettingsWrapper.OnLoaderInitialized += HandleXRInitComplete;
            

            _playerVRInputContainer.ResetView.OnPressed += HandleResetViewPressed;
            _playerVRInputContainer.ResetView.OnReleased += HandleResetViewReleased;

            _handControllerLeft.HandleOnEnable();
            _handControllerRight.HandleOnEnable();
            _feetInteractorVR.HandleOnEnable();

            _primaryUIService?.MovePrimaryUIToHolderRect(_primaryUIHolderRect);
            _secondaryUIService?.DisableShowHideKeyboardControl();
        }

        public void DeactivatePlayer()
        {
            if (_playerGO != null)
                _playerGO?.SetActive(false);

            _xrManagerSettingsWrapper.StopSubsystems();

            _playerVRInputContainer.ResetView.OnPressed -= HandleResetViewPressed;
            _playerVRInputContainer.ResetView.OnReleased -= HandleResetViewReleased;

            _handControllerLeft.HandleOnDisable();
            _handControllerRight.HandleOnDisable();
            _feetInteractorVR.HandleOnDisable();
        }

        private void HandleXRInitComplete()
        {
            _xrManagerSettingsWrapper.OnLoaderInitialized -= HandleXRInitComplete;
            _xrManagerSettingsWrapper.StartSubsystems(); 
        }

        internal void HandleLocalAvatarColorChanged(Color newColor)
        {
            _handControllerLeft.HandleLocalAvatarColorChanged(newColor);
            _handControllerRight.HandleLocalAvatarColorChanged(newColor);
        }

        internal override void HandleUpdate()
        {
            base.HandleUpdate();
            _handControllerLeft.HandleUpdate();
            _handControllerRight.HandleUpdate();
        }

        private void HandleResetViewPressed()
        {
            //TODO:
        }

        private void HandleResetViewReleased()
        {
            //TODO:
        }

        private void HandlePrimaryUITogglePressed()
        {
            
        }

        public void TearDown()
        {
            if (_xrManagerSettingsWrapper.IsInitializationComplete)
            {
                _xrManagerSettingsWrapper.StopSubsystems();
                _xrManagerSettingsWrapper.DeinitializeLoader();
            }
        }
    }
}
