using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.Platform.Internal
{
    internal class PlayerBrowserWorldHandler
    {
        public PlayerBrowserWorldInfo WorldInfo { get; private set; }
        private PlayerBrowserWorldView _worldView;

        private PlayerBrowserInstanceHandler _localInstanceHandler;
        private Dictionary<string, PlayerBrowserInstanceHandler> _remoteInstanceHandlers = new();

        public PlayerBrowserWorldHandler(VerticalLayoutGroup worldLayoutGroup, PlayerBrowserWorldInfo worldInfo)
        {
            //Instantiates the prefab, adds it as a tab to the primary UI service, and destroys the holder.
            GameObject worldInfoUIHolder = GameObject.Instantiate(Resources.Load<GameObject>("PlayerBrowserWorldInfoUIHolder"));
            GameObject worldInfoUI = worldInfoUIHolder.transform.GetChild(0).gameObject;
            worldInfoUI.SetActive(false);

            worldInfoUI.transform.SetParent(worldLayoutGroup.transform, false);
            GameObject.Destroy(worldInfoUIHolder);

           // WorldInfo = worldInfo;

            _worldView.SetName(worldInfo.WorldName);
            UpdateWorldInfo(worldInfo);
        }

        public void UpdateWorldInfo(PlayerBrowserWorldInfo newWorldInfo)
        {
            //WorldInfo = newWorldInfo;

            if (newWorldInfo.LocalInstanceInfo != null && _localInstanceHandler == null)
            {
                // Create local instance handler
                _localInstanceHandler = new PlayerBrowserInstanceHandler(_worldView.InstancesLayoutGroup, newWorldInfo.LocalInstanceInfo);
            }
            else if (newWorldInfo.LocalInstanceInfo == null && _localInstanceHandler != null)
            {
                // Destroy local instance handler
                _localInstanceHandler.Destroy();
                _localInstanceHandler = null;
            }

            //Create/update worlds===============================
            foreach (KeyValuePair<InstanceCode, PlatformInstanceInfo> newKVP in newWorldInfo.ins)
            {
                if (_worldHandlers.ContainsKey(newKVP.Key))
                {
                    // Update existing world handler
                    _worldHandlers[newKVP.Key].UpdateWorldInfo(newKVP.Value);
                }
                else
                {
                    // Create new world handler
                    PlayerBrowserWorldHandler newWorldHandler = new(_playerBrowserView.WorldLayoutGroup, newKVP.Value);
                    _worldHandlers.Add(newKVP.Key, newWorldHandler);
                }
            }

            //Remove worlds that no longer exist===============================
            foreach (KeyValuePair<string, PlayerBrowserWorldHandler> existingKVP in _worldHandlers)
            {
                if (!newWorldInfos.ContainsKey(existingKVP.Key))
                {
                    existingKVP.Value.Destroy();
                    _worldHandlers.Remove(existingKVP.Key);
                }
            }
        }

        public void Destroy()
        {
            GameObject.Destroy(_worldView.gameObject);
        }
    }
}
