using UnityEngine;

namespace VE2.Core.Player.Internal
{
    public class PlayerVRReferences : MonoBehaviour
    {
        public Transform RootTransform => _rootTransform;
        [SerializeField] private Transform _rootTransform;

        public Transform VerticalOffsetTransform => _verticalOffsetTransform;
        [SerializeField] private Transform _verticalOffsetTransform;

        public Transform HeadTransform => _headTransform;
        [SerializeField] private Transform _headTransform;   

        internal V_CollisionDetector FeetCollisionDetector => _feetCollisionDetector;
        [SerializeField] private V_CollisionDetector _feetCollisionDetector;

        public RectTransform PrimaryUIHolderRect => _primaryUIHolderRect;     
        [SerializeField] private RectTransform _primaryUIHolderRect;
    }
}
