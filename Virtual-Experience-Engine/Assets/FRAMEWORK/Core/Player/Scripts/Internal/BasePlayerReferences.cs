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
    }
}
