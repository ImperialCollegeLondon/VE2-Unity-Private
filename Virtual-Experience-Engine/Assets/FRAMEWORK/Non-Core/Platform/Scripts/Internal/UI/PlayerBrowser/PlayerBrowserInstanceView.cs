using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.Platform.Internal
{
    internal class PlayerBrowserInstanceView : MonoBehaviour
    {
        [SerializeField] public VerticalLayoutGroup PlayersLayoutGroup;

        [SerializeField] private TMP_Text _instanceNumberText;
        [SerializeField] private Button _instanceButton;

        public event Action OnInstanceButtonClicked;

        public void Setup(InstanceCode instanceCode)
        {
            _instanceNumberText.text = instanceCode.ToString();
            _instanceButton.onClick.AddListener(() => OnInstanceButtonClicked?.Invoke());
        }
    }
}
