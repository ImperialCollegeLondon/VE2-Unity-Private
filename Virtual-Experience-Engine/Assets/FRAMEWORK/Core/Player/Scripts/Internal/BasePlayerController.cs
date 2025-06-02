using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace VE2.Core.Player.Internal
{
    internal class BasePlayerController
    {
        internal Camera Camera;
        protected CollisionDetector _FeetCollisionDetector;
        protected Transform _PlayerHeadTransform;
        protected Transform _rootTransform;

        internal virtual void HandleUpdate()
        {
            if (Physics.Raycast(_PlayerHeadTransform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
                _FeetCollisionDetector.transform.position = hit.point;
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
            cameraData.antialiasingQuality = cameraConfig.AntiAliasingQuality;;
            cameraData.renderPostProcessing = cameraConfig.EnablePostProcessing;
        }

        public virtual void SetPlayerPosition(Vector3 position)
        {
            _rootTransform.position = position;
        }

        public Vector3 GetPlayerPosition()
        {
            return _rootTransform.position;
        }
    }
}
