using UnityEngine;

namespace VE2.Core.Player.Internal
{
    internal class PlayerVRReferences : MonoBehaviour
    {
        internal Camera Camera => _camera;
        [SerializeField] private Camera _camera;

        internal Transform RootTransform => _rootTransform;
        [SerializeField] private Transform _rootTransform;

        internal Transform VerticalOffsetTransform => _verticalOffsetTransform;
        [SerializeField] private Transform _verticalOffsetTransform;

        internal Transform HeadTransform => _headTransform;
        [SerializeField] private Transform _headTransform;   

        internal Collider FeetCollider => _feetCollider;
        [SerializeField] private Collider _feetCollider;

        internal RectTransform PrimaryUIHolderRect => _primaryUIHolderRect;     
        [SerializeField] private RectTransform _primaryUIHolderRect;

        internal ResetViewUIHandler ResetViewUIHandler => _resetViewUIHandler;
        [SerializeField] private ResetViewUIHandler _resetViewUIHandler;
    }
}
