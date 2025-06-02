using UnityEngine;
using VE2.Common.API;
using VE2.Core.Player.API;

namespace VE2.Core.Player.Internal
{
    internal class V_Billboard : MonoBehaviour
    {
        [SerializeField, Tooltip("How fast the billboard rotates to face the camera (higher = snappier)")]
        private float followSpeed = 5f;

        private Transform _cameraTransform;
        private Transform CameraTransform 
        {
            get 
            {
                if (_cameraTransform != null && _cameraTransform.gameObject.activeInHierarchy)
                    return _cameraTransform;

                if (VE2API.Player != null)
                    _cameraTransform = VE2API.Player.ActiveCamera.transform;
                else
                    _cameraTransform = Camera.main?.transform;

                return _cameraTransform;
            }
        }

        void LateUpdate()
        {
            if (CameraTransform == null)
                return;

            // Determine the desired forward vector
            Vector3 directionToCamera = transform.position - CameraTransform.position;
            Vector3 flatDirection = Vector3.ProjectOnPlane(directionToCamera, Vector3.up);

            if (flatDirection.sqrMagnitude < 0.001f)
                return; // Avoid jitter when very close

            Quaternion targetRotation = Quaternion.LookRotation(flatDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, followSpeed * Time.deltaTime);
        }
    }
}
