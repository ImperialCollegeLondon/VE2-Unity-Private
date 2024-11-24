using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.Player
{
    public class PlayerControllerVR : PlayerController
    {
        [SerializeField] private Transform _headTransform;
        // [SerializeField] private InteractorVR interactorVRLeft; 
        // [SerializeField] private InteractorVR interactorVRRight; 

        private V_HandController _handControllerLeft;
        private V_HandController _handControllerRight;

        [SerializeField] private Transform _leftHandHolder;
        [SerializeField] private Transform _rightHandHolder;

        public override PlayerTransformData PlayerTransformData
        {
            get
            {
                return new PlayerTransformData(
                    true,
                    transform.position,
                    transform.rotation,
                    _headTransform.transform.localPosition,
                    _headTransform.transform.rotation,
                    _handControllerLeft.GrabberTransform.localPosition,
                    _handControllerLeft.GrabberTransform.localRotation,
                    _handControllerRight.GrabberTransform.localPosition,
                    _handControllerRight.GrabberTransform.localRotation
                );
            }
        }

        private PlayerVRControlConfig _controlConfig;
        private IXRManagerWrapper _xrManagerSettingsWrapper;
        private PlayerVRInputContainer _playerVRInputContainer;

        //TODO: Ideally, this would be a constructor
        public void Initialize(InteractorContainer interactorContainer, PlayerVRControlConfig controlConfig, IMultiplayerSupport multiplayerSupport, PlayerVRInputContainer playerVRInputContainer, IRaycastProvider raycastProvider, IXRManagerWrapper xrManagerSettingsWrapper)
        {
            _controlConfig = controlConfig;
            _xrManagerSettingsWrapper = xrManagerSettingsWrapper;
            _playerVRInputContainer = playerVRInputContainer;

            GameObject handVRLeftPrefab = Resources.Load<GameObject>("HandVRLeft");
            GameObject handVRLeftGO = GameObject.Instantiate(handVRLeftPrefab, _leftHandHolder, false);
            GameObject handVRRightGO = GameObject.Instantiate(handVRLeftPrefab, _rightHandHolder, false);
            handVRRightGO.transform.localScale = new Vector3(-1, 1, 1);

            _handControllerLeft = new V_HandController(interactorContainer, transform, handVRLeftGO, playerVRInputContainer.HandVRLeftInputContainer, InteractorType.LeftHandVR, multiplayerSupport, raycastProvider);
            _handControllerRight = new V_HandController(interactorContainer, transform, handVRRightGO, playerVRInputContainer.HandVRRightInputContainer, InteractorType.RightHandVR, multiplayerSupport, raycastProvider);
        }

        public override void ActivatePlayer(PlayerTransformData initTransformData)
        {
            base.ActivatePlayer(initTransformData);
            transform.SetPositionAndRotation(initTransformData.RootPosition, initTransformData.RootRotation);
            _headTransform.transform.SetLocalPositionAndRotation(initTransformData.HeadLocalPosition, initTransformData.HeadLocalRotation);

            _xrManagerSettingsWrapper.InitializeLoader();
            _xrManagerSettingsWrapper.StartSubsystems();

            _playerVRInputContainer.ResetView.OnPressed += HandleResetViewPressed;
            _playerVRInputContainer.ResetView.OnReleased += HandleResetViewReleased;

            _handControllerLeft.HandleOnEnable();
            _handControllerRight.HandleOnEnable();
        }

        public override void DeactivatePlayer()
        {
            base.DeactivatePlayer();

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

        private void OnDestroy()
        {
            if (_xrManagerSettingsWrapper.IsInitializationComplete)
            {
                _xrManagerSettingsWrapper.StopSubsystems();
                _xrManagerSettingsWrapper.DeinitializeLoader();
            }
        }
    }
}
