using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.Platform.Internal
{
    internal class PlayerBrowserPlayerHandler
    {
        private PlayerBrowserPlayerView _playerView;

        public PlayerBrowserPlayerHandler(VerticalLayoutGroup instanceLayoutGroup, ClientInfoBase clientInfo, bool isHost)
        {
            //Instantiates the prefab, adds it as a tab to the primary UI service, and destroys the holder.
            GameObject worldInfoUIHolder = GameObject.Instantiate(Resources.Load<GameObject>("PlayerBrowserWorldInfoUIHolder"));
            GameObject worldInfoUI = worldInfoUIHolder.transform.GetChild(0).gameObject;
            worldInfoUI.SetActive(false);

            worldInfoUI.transform.SetParent(instanceLayoutGroup.transform, false);
            GameObject.Destroy(worldInfoUIHolder);


            _playerView.Setup(clientInfo, isHost);
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
