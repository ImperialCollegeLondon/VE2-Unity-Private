using UnityEngine;

namespace VE2.Core.Player.Internal
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class BasePlayerReferences : MonoBehaviour
    {
        internal Camera Camera => _camera;
        [SerializeField] private Camera _camera;

        public RectTransform PrimaryUIHolderRect => _primaryUIHolderRect;
        [SerializeField, IgnoreParent] private RectTransform _primaryUIHolderRect;

        internal Transform HeadTransform => _headTransform;
        [SerializeField] private Transform _headTransform;   

        internal Transform TorsoTransform => _torsoTransform;
        [SerializeField] private Transform _torsoTransform;

        //Then what about vr vs 2d
        //No hands in 2d, how do we even tell the avatar handler whether to use hands?

        internal Collider FeetCollider => _feetCollider;
        [SerializeField] private Collider _feetCollider;
    }
}
