using TMPro;
using UnityEngine;

namespace VE2.Core.UI.Internal
{
    internal class SecondaryUIView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _clockText; 
        [SerializeField] private TMP_Text _controlPromptText;
        [SerializeField] private GameObject _bottomPanel; 
        [SerializeField] private GameObject _contentPanel;
        [SerializeField] private GameObject __defaultContent;

        internal void SetContent(RectTransform content)
        {
            UIUtils.MovePanelToFillRect(content.GetComponent<RectTransform>(), _contentPanel.GetComponent<RectTransform>());
            content.gameObject.SetActive(true);
            __defaultContent.SetActive(false);
        }

        private void Update()
        {
            _clockText.text = System.DateTime.Now.Hour.ToString("D2") + ":" + System.DateTime.Now.Minute.ToString("D2");
            
        }
    }
}
