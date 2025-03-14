using UnityEngine;
using UnityEngine.UI;
using VE2.Core.Common;

namespace VE2.Core.UI.Internal
{
    internal class PrimaryUIReferences : MonoBehaviour
    {
        public GameObject PrimaryUI => gameObject;

        [IgnoreParent] public CenterPanelUIReferences CenterPanelUIReferences;

        public Button CloseButton;
    }
}


