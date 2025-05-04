using UnityEngine;

namespace VE2.Core.Player.Internal
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class PlayerVRReferences : BasePlayerReferences
    {
        internal Transform RootTransform => _rootTransform;
        [SerializeField] private Transform _rootTransform;

        internal Transform VerticalOffsetTransform => _verticalOffsetTransform;
        [SerializeField] private Transform _verticalOffsetTransform;

        internal Transform HeadTransform => _headTransform;
        [SerializeField] private Transform _headTransform;   

        internal Collider FeetCollider => _feetCollider;
        [SerializeField] private Collider _feetCollider;

        internal ResetViewUIHandler ResetViewUIHandler => _resetViewUIHandler;
        [SerializeField] private ResetViewUIHandler _resetViewUIHandler;

        internal Transform NeutralPositionOffsetTransform => _neutralPositionOffsetTransform;
        [SerializeField] private Transform _neutralPositionOffsetTransform;
    }
}
