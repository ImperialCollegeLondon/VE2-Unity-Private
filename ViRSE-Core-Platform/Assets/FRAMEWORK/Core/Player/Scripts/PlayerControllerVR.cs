using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Management;
using ViRSE.Core.Shared;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.Core.Player
{
    public class PlayerControllerVR : PlayerController
    {
        [SerializeField] private Transform _headTransform;
        [SerializeField] private InteractorVR interactorVRLeft; 
        [SerializeField] private InteractorVR interactorVRRight; 
        private PlayerVRControlConfig _controlConfig;
        private XRManagerSettings _xrManagerSettings;

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

        public void Initialize(PlayerVRControlConfig controlConfig, IMultiplayerSupport multiplayerSupport, IInputHandler inputHandler, IRaycastProvider raycastProvider, XRManagerSettings xrManagerSettings)
        {
            _controlConfig = controlConfig;
            _xrManagerSettings = xrManagerSettings;
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
            _xrManagerSettings.StartSubsystems();
        }

        public override void DeactivatePlayer()
        {
            gameObject.SetActive(false);
            _xrManagerSettings.StopSubsystems();
        }
    }
}
