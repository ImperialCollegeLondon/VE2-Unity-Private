using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VE2.Core.Common;

namespace VE2.NonCore.Platform.Internal
{
    public class V_PlatformQuickUIView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _playerNameText;

        [SerializeField] private TMP_Text _connectedText;
        [SerializeField] private TMP_Text _domainText;
        [SerializeField] private TMP_Text _pingText;
        [SerializeField] private TMP_Text _hostshipText;

        [SerializeField] private Button _backToHubButton;
        [SerializeField] private Button _toggleVoiceChatButton;

        internal event Action OnBackToHubClicked;
        internal event Action OnToggleVoiceChatButtonClicked;

        private ColorConfiguration _colorConfiguration;

        internal void SetPlayerNameText(string playerName) => _playerNameText.text = playerName;

        internal void SetConnectedText(bool connected) 
        {
            _connectedText.text = connected ? "Connected" : "Not Connected";
            _connectedText.color = connected ? _colorConfiguration.AccentPrimaryColor : _colorConfiguration.AccentSecondaryColor;
        }
        internal void SetConnectionNA() 
        {
            _connectedText.text = "Single-Player";
            _connectedText.color = _colorConfiguration.SecondaryColor;
        }
        

        internal void SetHost() => _hostshipText.text = "Host";
        internal void SetNonHost() => _hostshipText.text = "Non-Host";
        internal void SetHostNA() => _hostshipText.text = "N/A";

        internal void SetPingTextMS(int ping) => _pingText.text = ping.ToString() + "ms";
        internal void SetPingTextNA() => _pingText.text = "N/A";

        private PlatformQuickUIHandler _handler;

        private void Start()
        {
            _backToHubButton.onClick.AddListener(HandleBackToHubButtonClicked);
            _toggleVoiceChatButton.onClick.AddListener(HandleToggleVoiceChatButtonClicked);
            _colorConfiguration = Resources.Load<ColorConfiguration>("ColorConfiguration"); //TODO: Inject

            _handler = new(this);
        }

        private void Update()
        {
            _handler.HandleUpdate();
        }

        private void HandleBackToHubButtonClicked()
        {
            EventSystem.current.SetSelectedGameObject(null); //Deselect the button to prevent it from being highlighted
            Debug.Log("Back to Hub clicked");
            OnBackToHubClicked?.Invoke();
        }

        private void HandleToggleVoiceChatButtonClicked()
        {
            EventSystem.current.SetSelectedGameObject(null); //Deselect the button to prevent it from being highlighted
            Debug.Log("Toggle Voice Chat clicked");
            OnToggleVoiceChatButtonClicked?.Invoke();

            //TODO - change visual
        }

        /*
            Server:Live should say Domain: Imperial, or Domain: Debug etc 
        */
    }
}
