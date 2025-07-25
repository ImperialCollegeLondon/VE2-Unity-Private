using System;
using UnityEngine;
using UnityEngine.UI;

namespace VE2.NonCore.Platform.Internal
{
    public class PlayerBrowserView : MonoBehaviour
    {
        [SerializeField] private Button _refreshButton;
        [SerializeField] public VerticalLayoutGroup WorldLayoutGroup;

        public event Action OnRefreshButtonClicked;

        private void Awake()
        {
            _refreshButton.onClick.AddListener(() => OnRefreshButtonClicked?.Invoke());
        }
    }
}
