using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VE2.Common.Shared;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.Platform.Internal
{
    internal class PlayerBrowserPlayerHandler
    {
        public event Action<ushort> OnPlayerInfoButtonClicked;

        private readonly PlayerBrowserPlayerView _playerView;

        public PlayerBrowserPlayerHandler(VerticalLayoutGroup instanceLayoutGroup, ClientInfoBase clientInfo, bool isHost)
        {
            //Instantiates the prefab, adds it as a tab to the primary UI service, and destroys the holder.
            GameObject playerInfoUIHolder = CommonUtils.SpawnUIPanelFromResourcesAndMoveToParent("PlayerBrowserPlayerInfo", instanceLayoutGroup.transform);
            _playerView = playerInfoUIHolder.GetComponent<PlayerBrowserPlayerView>();

            _playerView.Setup(clientInfo, isHost);
            UpdatePlayerInfo(clientInfo, isHost);
            _playerView.OnPlayerInfoButtonClicked += () => OnPlayerInfoButtonClicked?.Invoke(clientInfo.ClientID);
        }

        public void UpdatePlayerInfo(ClientInfoBase clientInfo, bool isHost)
        {
            _playerView.UpdatePlayerInfo(clientInfo, isHost);
        }

        public void Destroy()
        {
            GameObject.Destroy(_playerView.gameObject);
        }
    }
}
