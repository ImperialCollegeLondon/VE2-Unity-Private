using UnityEngine;

namespace VE2.Core.Player.Internal
{
    internal class BasePlayerController
    {
        protected V_CollisionDetector _feetCollisionDetector;
        protected Transform _playerHeadTransform; 

        internal virtual void HandleUpdate()
        {
            if (Physics.Raycast(_playerHeadTransform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
                _feetCollisionDetector.transform.position = hit.point;
        }
    }
}
