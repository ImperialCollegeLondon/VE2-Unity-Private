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

        //TODO: Ideally, this would be a constructor
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
            GameObject handVRLeftGO = GameObject.Instantiate(handVRLeftPrefab, _headTransform, false);
            GameObject handVRRightGO = GameObject.Instantiate(handVRLeftPrefab, _headTransform, false);
            handVRRightGO.transform.localScale = new Vector3(-1, 1, 1);

            _handControllerLeft = new V_HandController(interactorContainer, _rootTransform, _verticalOffsetTransform, handVRLeftGO, playerVRInputContainer.HandVRLeftInputContainer, InteractorType.LeftHandVR, multiplayerSupport, raycastProvider);
            _handControllerRight = new V_HandController(interactorContainer, _rootTransform, _verticalOffsetTransform, handVRRightGO, playerVRInputContainer.HandVRRightInputContainer, InteractorType.RightHandVR, multiplayerSupport, raycastProvider);
        }

        public void ActivatePlayer(PlayerTransformData initTransformData)
        {
            _playerGO.SetActive(true);

            Debug.Log("ActivateVR, pos is " + initTransformData.RootPosition);
            _rootTransform.SetPositionAndRotation(initTransformData.RootPosition, initTransformData.RootRotation);
            _verticalOffsetTransform.localPosition = new Vector3(0, initTransformData.VerticalOffset, 0);
            _headTransform.transform.SetLocalPositionAndRotation(initTransformData.HeadLocalPosition, initTransformData.HeadLocalRotation);

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
