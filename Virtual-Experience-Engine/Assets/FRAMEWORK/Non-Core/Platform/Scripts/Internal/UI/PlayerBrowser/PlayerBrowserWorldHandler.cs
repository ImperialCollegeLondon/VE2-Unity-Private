using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VE2.Common.Shared;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.Platform.Internal
{
    internal class PlayerBrowserWorldHandler
    {
        public event Action<string> OnWorldButtonClicked;

        //Note, key needs to be the stringified instance code (val type) rather than the InstanceCode obj itself (ref type)
        private Dictionary<string, PlayerBrowserInstanceHandler> _remoteInstanceHandlers = new();
        private PlayerBrowserInstanceHandler _localInstanceHandler; //Null if no local instance in this world

        private readonly PlayerBrowserWorldView _worldView;

        public PlayerBrowserWorldHandler(VerticalLayoutGroup worldLayoutGroup, PlayerBrowserWorldInfo worldInfo)
        {
            GameObject worldUI = CommonUtils.SpawnUIPanelFromResourcesAndMoveToParent("PlayerBrowserWorldInfo", worldLayoutGroup.transform);
            _worldView = worldUI.GetComponent<PlayerBrowserWorldView>();

            _worldView.Setup(worldInfo.WorldName);
            _worldView.OnWorldButtonClicked += () => OnWorldButtonClicked?.Invoke(worldInfo.WorldName);
            UpdateWorldInfo(worldInfo);
        }

        public void UpdateWorldInfo(PlayerBrowserWorldInfo newWorldInfo)
        {
            //Create/update/remove local instance info================================
            if (newWorldInfo.LocalInstanceInfo != null && _localInstanceHandler == null)
            {
                // Create local instance handler
                _localInstanceHandler = new PlayerBrowserInstanceHandler(_worldView.InstancesLayoutGroup, newWorldInfo.LocalInstanceInfo);
            }
            else if (newWorldInfo.LocalInstanceInfo != null && _localInstanceHandler != null)
            {
                _localInstanceHandler.UpdateInstanceInfo(newWorldInfo.LocalInstanceInfo);
            }
            else if (newWorldInfo.LocalInstanceInfo == null && _localInstanceHandler != null)
            {
                // Destroy local instance handler
                _localInstanceHandler.Destroy();
                _localInstanceHandler = null;
            }

            //Create/update remote instances===========================================
            foreach (KeyValuePair<string, PlatformInstanceInfo> newKVP in newWorldInfo.RemoteInstanceInfo)
            {
                if (_remoteInstanceHandlers.ContainsKey(newKVP.Key))
                {
                    // Update existing instance handler
                    _remoteInstanceHandlers[newKVP.Key].UpdateInstanceInfo(newKVP.Value);
                }
                else
                {
                    // Create new instance handler
                    PlayerBrowserInstanceHandler newInstanceHandler = new(_worldView.InstancesLayoutGroup, newKVP.Value);
                    _remoteInstanceHandlers.Add(newKVP.Key.ToString(), newInstanceHandler);
                }
            }

            //Remove remote instances that no longer exist===============================
            foreach (KeyValuePair<string, PlayerBrowserInstanceHandler> existingKVP in _remoteInstanceHandlers)
            {
                if (!newWorldInfo.RemoteInstanceInfo.ContainsKey(existingKVP.Key.ToString()))
                {
                    existingKVP.Value.Destroy();
                    _remoteInstanceHandlers.Remove(existingKVP.Key);
                }
            }
        }

        public void Destroy()
        {
            GameObject.Destroy(_worldView.gameObject);
        }
    }
}
