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
        //Sets the name text
        //Shows icons for host or admin 
        //Has the player info button 

        [SerializeField] private TMP_Text _playerNameText;
        [SerializeField] private Image _hostIcon;
        [SerializeField] private Image _adminIcon;
        [SerializeField] private Button _playerInfoButton;

        public event Action<ushort> OnPlayerInfoButtonClicked;

        private ushort _playerId;

        private void Awake()
        {
            _playerInfoButton.onClick.AddListener(() => OnPlayerInfoButtonClicked?.Invoke(_playerId));
        }

        public void Setup(ClientInfoBase clientInfo, bool isHost)
        {
            _playerId = clientInfo.ClientID;

            UpdatePlayerInfo(clientInfo, isHost);
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
