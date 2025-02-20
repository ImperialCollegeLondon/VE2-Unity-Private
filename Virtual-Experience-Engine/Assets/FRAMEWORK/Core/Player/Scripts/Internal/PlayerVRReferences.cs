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
    }
}
