using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.Platform.Internal
{
    internal class PlayerBrowserInstanceHandler
    {
        //public PlayerBrowserWorldInfo WorldInfo { get; private set; }
        private PlayerBrowserInstanceView _instanceView;

        private Dictionary<string, PlayerBrowserPlayerHandler> _playerHandlers = new();

        public PlayerBrowserInstanceHandler(VerticalLayoutGroup worldLayoutGroup, InstanceInfoBase instanceInfo)
        {
            //Instantiates the prefab, adds it as a tab to the primary UI service, and destroys the holder.
            GameObject worldInfoUIHolder = GameObject.Instantiate(Resources.Load<GameObject>("PlayerBrowserWorldInfoUIHolder"));
            GameObject worldInfoUI = worldInfoUIHolder.transform.GetChild(0).gameObject;
            worldInfoUI.SetActive(false);

            worldInfoUI.transform.SetParent(worldLayoutGroup.transform, false);
            GameObject.Destroy(worldInfoUIHolder);

            _instanceView.SetName(instanceInfo.WorldName);
            UpdateInstanceInfo(instanceInfo);
        }

        public void UpdateInstanceInfo(PlayerBrowserInstanceInfo worldInfo)
        {
            
        }
    }
}
