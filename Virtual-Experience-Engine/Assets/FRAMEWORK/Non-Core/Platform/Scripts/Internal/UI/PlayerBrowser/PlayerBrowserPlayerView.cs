using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static VE2.NonCore.Instancing.API.InstancePublicSerializables;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.Platform.Internal
{
    internal class PlayerBrowserPlayerView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _playerNameText;
        [SerializeField] private Image _hostIcon;
        [SerializeField] private Image _adminIcon;
        [SerializeField] private Button _playerInfoButton;

        public event Action OnPlayerInfoButtonClicked;

        public void Setup(ClientInfoBase clientInfo, bool isHost)
        {
            UpdatePlayerInfo(clientInfo, isHost);
            _playerInfoButton.onClick.AddListener(() => OnPlayerInfoButtonClicked?.Invoke());
        }

        public void UpdatePlayerInfo(ClientInfoBase clientInfo, bool isHost)
        {
            if (clientInfo is PlatformClientInfo platformClientInfo)
            {
                _playerNameText.text = platformClientInfo.PlayerPresentationConfig.PlayerName;
                _adminIcon.gameObject.SetActive(false);
            }
            else if (clientInfo is InstancedClientInfo instancedClientInfo)
            {
                _playerNameText.text = instancedClientInfo.AvatarAppearanceWrapper.InstancedAvatarAppearance.BuiltInPresentationConfig.PlayerName;
                _adminIcon.gameObject.SetActive(clientInfo.IsAdmin);
            }

            _hostIcon.gameObject.SetActive(isHost);
        }
    }
}
