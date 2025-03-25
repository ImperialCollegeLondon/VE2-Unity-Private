using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VE2.Core.UI.Internal
{
    internal class PrimaryUITopBarView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _subtitleText;
        [SerializeField] private Button _closeButton;

        internal event Action OnCloseButtonClicked;

        internal string TitleText { set => _titleText.text = value; }
        internal string SubtitleText { set => _subtitleText.text = value; }

        private void Awake()
        {
            _closeButton.onClick.AddListener(() => OnCloseButtonClicked?.Invoke());

            //TODO - info ticker loop
        }
    }
}
