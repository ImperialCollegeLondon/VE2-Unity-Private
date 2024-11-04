using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.Player
{
    public class PlayerControllerVR : PlayerController
    {
        [SerializeField] private Transform _headTransform;
        [SerializeField] private InteractorVR interactorVRLeft; 
        [SerializeField] private InteractorVR interactorVRRight; 
        private PlayerVRControlConfig _controlConfig;
        private IXRManagerWrapper _xrManagerSettingsWrapper;

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

        public void Initialize(PlayerVRControlConfig controlConfig, IMultiplayerSupport multiplayerSupport, IInputHandler inputHandler, IRaycastProvider raycastProvider, IXRManagerWrapper xrManagerSettingsWrapper)
        {
            _controlConfig = controlConfig;
            _xrManagerSettingsWrapper = xrManagerSettingsWrapper;
            //interactorVRLeft.Initialize();
            //interactorVRRight.Initialize();
        }


        public override void ActivatePlayer(PlayerTransformData initTransformData)
        {
            transform.SetPositionAndRotation(initTransformData.RootPosition, initTransformData.RootRotation);
            _headTransform.transform.SetLocalPositionAndRotation(initTransformData.HeadLocalPosition, initTransformData.HeadLocalRotation);
            interactorVRLeft.GrabberTransform.SetLocalPositionAndRotation(initTransformData.HandVRLeftLocalPosition, initTransformData.HandVRLeftLocalRotation);
            interactorVRLeft.GrabberTransform.SetLocalPositionAndRotation(initTransformData.HandVRRightLocalPosition, initTransformData.HandVRRightLocalRotation);

            gameObject.SetActive(true);
            _xrManagerSettingsWrapper.StartSubsystems();
        }

        public override void DeactivatePlayer()
        {
            gameObject.SetActive(false);
            _xrManagerSettingsWrapper.StopSubsystems();
        }
    }
}
