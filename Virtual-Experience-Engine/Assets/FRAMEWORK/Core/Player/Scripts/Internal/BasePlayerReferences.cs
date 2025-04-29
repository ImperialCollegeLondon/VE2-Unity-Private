using UnityEngine;

namespace VE2.Core.Player.Internal
{
    internal class BasePlayerReferences : MonoBehaviour
    {
        internal Camera Camera => _camera;
        [SerializeField] private Camera _camera;

        public RectTransform PrimaryUIHolderRect => _primaryUIHolderRect;
        [SerializeField, IgnoreParent] private RectTransform _primaryUIHolderRect;
    }
}
