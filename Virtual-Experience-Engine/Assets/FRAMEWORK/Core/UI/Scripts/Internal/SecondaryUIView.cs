using TMPro;
using UnityEngine;
using VE2.Common.Shared;

namespace VE2.Core.UI.Internal
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class SecondaryUIView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _clockText; 
        [SerializeField] private TMP_Text _controlPromptText;
        [SerializeField] private GameObject _bottomPanel; 
        [SerializeField] private GameObject _contentPanel;
        [SerializeField] private GameObject __defaultContent;

        internal void SetContent(RectTransform content)
        {
            CommonUtils.MovePanelToFillRect(content.GetComponent<RectTransform>(), _contentPanel.GetComponent<RectTransform>());
            content.gameObject.SetActive(true);
            __defaultContent.SetActive(false);
        }

        internal void EnableKeyPrompt() => _controlPromptText.gameObject.SetActive(true);
        internal void DisableKeyPrompt() => _controlPromptText.gameObject.SetActive(false);

        internal void ToggleBottomPanel() => _bottomPanel.SetActive(!_bottomPanel.activeSelf);

        private void Update()
        {
            _clockText.text = System.DateTime.Now.Hour.ToString("D2") + ":" + System.DateTime.Now.Minute.ToString("D2");
        }
    }
}
