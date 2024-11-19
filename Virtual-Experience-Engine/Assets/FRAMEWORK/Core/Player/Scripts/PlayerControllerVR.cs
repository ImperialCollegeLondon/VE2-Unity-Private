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
        [SerializeField] private InteractorVR interactorVRLeft; 
        [SerializeField] private InteractorVR interactorVRRight; 

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
                    interactorVRLeft.GrabberTransform.localPosition,
                    interactorVRLeft.GrabberTransform.localRotation,
                    interactorVRRight.GrabberTransform.localPosition,
                    interactorVRRight.GrabberTransform.localRotation
                );
            }
        }

        private PlayerVRControlConfig _controlConfig;
        private IXRManagerWrapper _xrManagerSettingsWrapper;
        private PlayerVRInputContainer _playerVRInputContainer;

        //TODO: Ideally, this would be a constructor
        public void Initialize(PlayerVRControlConfig controlConfig, IMultiplayerSupport multiplayerSupport, PlayerVRInputContainer playerVRInputContainer, IRaycastProvider raycastProvider, IXRManagerWrapper xrManagerSettingsWrapper)
        {
            _controlConfig = controlConfig;
            _xrManagerSettingsWrapper = xrManagerSettingsWrapper;
            _playerVRInputContainer = playerVRInputContainer;

            //TODO, instantiate the InteractorVRObjects, create one, mirror it for the second 

            interactorVRLeft.Initialize(interactorVRLeft.transform, InteractorType.LeftHandVR, multiplayerSupport, _playerVRInputContainer.InteractorVRLeftInputContainer, raycastProvider);
            interactorVRRight.Initialize(interactorVRLeft.transform, InteractorType.RightHandVR, multiplayerSupport, _playerVRInputContainer.InteractorVRLeftInputContainer, raycastProvider);
        }

        public override void ActivatePlayer(PlayerTransformData initTransformData)
        {
            transform.SetPositionAndRotation(initTransformData.RootPosition, initTransformData.RootRotation);
            _headTransform.transform.SetLocalPositionAndRotation(initTransformData.HeadLocalPosition, initTransformData.HeadLocalRotation);
            interactorVRLeft.GrabberTransform.SetLocalPositionAndRotation(initTransformData.HandVRLeftLocalPosition, initTransformData.HandVRLeftLocalRotation);
            interactorVRLeft.GrabberTransform.SetLocalPositionAndRotation(initTransformData.HandVRRightLocalPosition, initTransformData.HandVRRightLocalRotation);

            gameObject.SetActive(true);
            _xrManagerSettingsWrapper.StartSubsystems();

            _playerVRInputContainer.ResetView.OnPressed += HandleResetViewPressed;
            _playerVRInputContainer.ResetView.OnReleased += HandleResetViewReleased;
            interactorVRLeft.HandleOnEnable();
            interactorVRRight.HandleOnEnable();
        }

        public override void DeactivatePlayer()
        {
            gameObject.SetActive(false);
            _xrManagerSettingsWrapper.StopSubsystems();

            _playerVRInputContainer.ResetView.OnPressed -= HandleResetViewPressed;
            _playerVRInputContainer.ResetView.OnReleased -= HandleResetViewReleased;
            interactorVRLeft.HandleOnDisable();
            interactorVRRight.HandleOnDisable();
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
