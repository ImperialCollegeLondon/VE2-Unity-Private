using UnityEngine;
using UnityEngine.Rendering.Universal;
using VE2.Common.Shared;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Player.Internal
{
    internal class BasePlayerController
    {
        public Vector3 PlayerPosition => _rootTransform.position;
        public virtual void SetPlayerPosition(Vector3 position) => _rootTransform.position = position;
        public Quaternion PlayerRotation => _rootTransform.rotation;
        public virtual void SetPlayerRotation(Quaternion rotation) => _rootTransform.rotation = rotation;

        internal PlayerAvatarHandler AvatarHandler;

        internal Camera Camera;
        protected CollisionDetector _FeetCollisionDetector;
        protected Transform _PlayerHeadTransform;
        protected Transform _rootTransform;

        internal virtual void HandleUpdate()
        {
            if (Physics.Raycast(_PlayerHeadTransform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
                _FeetCollisionDetector.transform.position = hit.point;

            AvatarHandler?.HandleUpdate();
        }

        protected void ConfigureCamera(CameraConfig cameraConfig)
        {
            Camera.fieldOfView = cameraConfig.FieldOfView2D;
            Camera.nearClipPlane = cameraConfig.NearClippingPlane;
            Camera.farClipPlane = cameraConfig.FarClippingPlane;
            Camera.cullingMask = cameraConfig.CullingMask;
            Camera.useOcclusionCulling = cameraConfig.OcclusionCulling;

            UniversalAdditionalCameraData cameraData = Camera.GetUniversalAdditionalCameraData();
            cameraData.antialiasing = cameraConfig.AntiAliasing;
            cameraData.antialiasingQuality = cameraConfig.AntiAliasingQuality; ;
            cameraData.renderPostProcessing = cameraConfig.EnablePostProcessing;
        }

        protected void OnClientIDReady(ushort clientID)
        {
            AvatarHandler.Enable(clientID);
        }
    }
}
