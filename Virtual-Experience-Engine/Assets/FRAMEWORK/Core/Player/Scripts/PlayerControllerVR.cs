using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.Player
{
    public class PlayerControllerVR
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
                    headRotation: _headTransform.transform.rotation,
                    handVRLeftPosition: _handControllerLeft.GrabberTransform.localPosition,
                    handVRLeftRotation: _handControllerLeft.GrabberTransform.localRotation,
                    handVRRightPosition: _handControllerRight.GrabberTransform.localPosition,
                    handVRRightRotation: _handControllerRight.GrabberTransform.localRotation
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

        private readonly V_HandController _handControllerLeft;
        private readonly V_HandController _handControllerRight;

        public PlayerControllerVR(InteractorContainer interactorContainer, PlayerVRInputContainer playerVRInputContainer, PlayerVRControlConfig controlConfig, 
            IRaycastProvider raycastProvider, IXRManagerWrapper xrManagerSettingsWrapper, IMultiplayerSupport multiplayerSupport)
        {
            GameObject playerVRPrefab = Resources.Load("vrPlayer") as GameObject;
            _playerGO = GameObject.Instantiate(playerVRPrefab, null, false);
            _playerGO.SetActive(false);

            _playerVRInputContainer = playerVRInputContainer;
            _controlConfig = controlConfig;
            _xrManagerSettingsWrapper = xrManagerSettingsWrapper;

            PlayerVRReferences playerVRReferences = _playerGO.GetComponent<PlayerVRReferences>();
            _rootTransform = playerVRReferences.RootTransform;
            _verticalOffsetTransform = playerVRReferences.VerticalOffsetTransform;
            _headTransform = playerVRReferences.HeadTransform;

            GameObject handVRLeftPrefab = Resources.Load<GameObject>("HandVRLeft");
            GameObject handVRLeftGO = GameObject.Instantiate(handVRLeftPrefab, _verticalOffsetTransform, false);
            GameObject handVRRightGO = GameObject.Instantiate(handVRLeftPrefab, _verticalOffsetTransform, false);
            handVRRightGO.transform.localScale = new Vector3(-1, 1, 1);
            handVRRightGO.name = "HandVRRight";

            _handControllerLeft = CreateHandController(handVRLeftGO, interactorContainer, playerVRInputContainer.HandVRLeftInputContainer, playerVRInputContainer.HandVRRightInputContainer.DragLocomotorInputContainer, InteractorType.LeftHandVR, raycastProvider, multiplayerSupport);
            _handControllerRight = CreateHandController(handVRRightGO, interactorContainer, playerVRInputContainer.HandVRRightInputContainer,playerVRInputContainer.HandVRLeftInputContainer.DragLocomotorInputContainer, InteractorType.RightHandVR, raycastProvider, multiplayerSupport);
        }

        private V_HandController CreateHandController(GameObject handGO, InteractorContainer interactorContainer, HandVRInputContainer handVRInputContainer, DragLocomotorInputContainer otherHandDragInputContainer, InteractorType interactorType, IRaycastProvider raycastProvider, IMultiplayerSupport multiplayerSupport)
        {
            V_HandVRReferences handVRReferences = handGO.GetComponent<V_HandVRReferences>();

            InteractorVR interactor = new(
                interactorContainer, handVRInputContainer.InteractorVRInputContainer,
                handVRReferences.InteractorVRReferences,
                interactorType, raycastProvider, multiplayerSupport);

            DragLocomotor dragLocomotor = new(
                handVRReferences.LocomotorVRReferences,
                handVRInputContainer.DragLocomotorInputContainer,
                otherHandDragInputContainer,
                _rootTransform, _verticalOffsetTransform, handGO.transform);

            SnapTurn snapTurn = new(
                handVRInputContainer.SnapTurnInputContainer,
                _rootTransform,
                handVRInputContainer.TeleportInputContainer);
            Teleport teleport = new(
                handVRInputContainer.TeleportInputContainer,
                _rootTransform, handVRReferences.InteractorVRReferences.RayOrigin);
            return new V_HandController(handGO, handVRInputContainer, interactor, dragLocomotor, snapTurn, teleport);
        }

        public void ActivatePlayer(PlayerTransformData initTransformData)
        {
            _playerGO.SetActive(true);

            _rootTransform.SetPositionAndRotation(initTransformData.RootPosition, initTransformData.RootRotation);
            _verticalOffsetTransform.localPosition = new Vector3(0, initTransformData.VerticalOffset, 0);
            //We don't set head transform here, tracking will override it anyway

            _xrManagerSettingsWrapper.InitializeLoader();
            _xrManagerSettingsWrapper.StartSubsystems();

            _playerVRInputContainer.ResetView.OnPressed += HandleResetViewPressed;
            _playerVRInputContainer.ResetView.OnReleased += HandleResetViewReleased;

            _handControllerLeft.HandleOnEnable();
            _handControllerRight.HandleOnEnable();
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
        }

        public void HandleLocalAvatarColorChanged(Color newColor)
        {
            _handControllerLeft.HandleLocalAvatarColorChanged(newColor);
            _handControllerRight.HandleLocalAvatarColorChanged(newColor);
        }

        public void HandleUpdate()
        {
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
