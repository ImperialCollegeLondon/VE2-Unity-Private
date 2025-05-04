using System;
using UnityEngine;
using UnityEngine.UI;

namespace VE2.Core.UI.Internal
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class PrimaryUIUtilsPanelView : MonoBehaviour
    {
        [SerializeField] private Button _quitButton; 
        [SerializeField] private Button _switchToVRButton;
        [SerializeField] private Button _switchTo2DButton;

        internal event Action OnQuitButtonClicked;
        internal event Action OnSwitchToVRButtonClicked;
        internal event Action OnSwitchTo2DButtonClicked;

        internal void EnableModeSwitchButtons()
        {
            _switchToVRButton.gameObject.SetActive(true);
            _switchTo2DButton.gameObject.SetActive(true);
        }

        internal void ShowSwitchToVRButton()
        {
            _switchToVRButton.gameObject.SetActive(true);
            _switchTo2DButton.gameObject.SetActive(false);
        }

        internal void ShowSwitchTo2DButton()
        {
            _switchToVRButton.gameObject.SetActive(false);
            _switchTo2DButton.gameObject.SetActive(true);
        }

        private void Awake()
        {
            //TODO - show confirm quit
            _quitButton.onClick.AddListener(() => OnQuitButtonClicked?.Invoke());

            _switchToVRButton.gameObject.SetActive(false);
            _switchTo2DButton.gameObject.SetActive(false);

            _switchToVRButton.onClick.AddListener(() => OnSwitchToVRButtonClicked?.Invoke());
            _switchTo2DButton.onClick.AddListener(() => OnSwitchTo2DButtonClicked?.Invoke());
        }
    }
}
