using UnityEngine;

namespace VE2.Core.Player.Internal
{
    public class PlayerVRReferences : MonoBehaviour
    {
        public Camera Camera => _camera;
        [SerializeField] private Camera _camera;

        public Transform RootTransform => _rootTransform;
        [SerializeField] private Transform _rootTransform;

        public Transform VerticalOffsetTransform => _verticalOffsetTransform;
        [SerializeField] private Transform _verticalOffsetTransform;

        public Transform HeadTransform => _headTransform;
        [SerializeField] private Transform _headTransform;   

        internal Collider FeetCollider => _feetCollider;
        [SerializeField] private Collider _feetCollider;

        public RectTransform PrimaryUIHolderRect => _primaryUIHolderRect;     
        [SerializeField] private RectTransform _primaryUIHolderRect;
    }
}
