using UnityEngine;
using VE2.Core.Player.API;

namespace VE2.Core.Player.Internal
{
    public class V_Billboard : MonoBehaviour
    {
        private Transform _cameraTransform;
        private Transform CameraTransform 
        {
            get 
            {
                if (_cameraTransform != null && _cameraTransform.gameObject.activeInHierarchy)
                    return _cameraTransform;

                if (PlayerAPI.Player != null)
                    _cameraTransform = PlayerAPI.Player.ActiveCamera.transform;
                else
                    _cameraTransform = Camera.main?.transform;

                return _cameraTransform;
            }
        }
        
        void Update()
        {
            if (CameraTransform == null)
                return;

            Vector3 vectorFromCamera = transform.position - CameraTransform.position;
            Vector3 forwardDirection = Vector3.ProjectOnPlane(vectorFromCamera, Vector3.up);
            transform.forward = forwardDirection;
        }
    }
}
