using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.Core.Player
{
    public class PlayerControllerVR : PlayerController
    {
        [SerializeField] private Camera cameraVR;
        [SerializeField] Interactor2D interactorVRLeft; //TODO should be a VR interactor
        [SerializeField] Interactor2D interactorVRRight; //TODO should be a VR interactor
        private PlayerVRControlConfig _controlConfig;

        public override PlayerTransformData PlayerTransformData
        {
            get
            {
                return new PlayerTransformData(
                    true,
                    transform.position,
                    transform.rotation,
                    cameraVR.transform.localPosition,
                    cameraVR.transform.rotation,
                    interactorVRLeft.GrabberTransform.localPosition,
                    interactorVRLeft.GrabberTransform.localRotation,
                    interactorVRRight.GrabberTransform.localPosition,
                    interactorVRRight.GrabberTransform.localRotation
                );
            }
        }

        public void Initialize(PlayerVRControlConfig controlConfig)
        {
            _controlConfig = controlConfig;
            //interactorVRLeft.Initialize();
            //interactorVRRight.Initialize();
        }


        public override void ActivatePlayer(PlayerTransformData initTransformData)
        {
            transform.SetPositionAndRotation(initTransformData.RootPosition, initTransformData.RootRotation);
            cameraVR.transform.SetLocalPositionAndRotation(initTransformData.HeadLocalPosition, initTransformData.HeadLocalRotation);
            interactorVRLeft.GrabberTransform.SetLocalPositionAndRotation(initTransformData.HandVRLeftLocalPosition, initTransformData.HandVRLeftLocalRotation);
            interactorVRLeft.GrabberTransform.SetLocalPositionAndRotation(initTransformData.HandVRRightLocalPosition, initTransformData.HandVRRightLocalRotation);
            gameObject.SetActive(true);
        }

        public override void DeactivatePlayer()
        {
            gameObject.SetActive(false);
        }
    }
}
