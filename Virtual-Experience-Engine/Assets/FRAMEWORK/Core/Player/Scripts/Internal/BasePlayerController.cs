using UnityEngine;

namespace VE2.Core.Player.Internal
{
    internal class BasePlayerController
    {
        protected V_CollisionDetector _FeetCollisionDetector;
        protected Transform _PlayerHeadTransform; 

        internal virtual void HandleUpdate()
        {
            if (Physics.Raycast(_PlayerHeadTransform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
                _FeetCollisionDetector.transform.position = hit.point;
        }
    }
}
