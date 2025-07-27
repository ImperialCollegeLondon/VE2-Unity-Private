using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VE2.Common.Shared;
using static VE2.NonCore.Instancing.API.InstancePublicSerializables;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.Platform.Internal
{
    internal class PlayerBrowserInstanceHandler
    {
        public event Action<InstanceCode> OnInstanceButtonClicked;

        private Dictionary<ushort, PlayerBrowserPlayerHandler> _playerHandlers = new();
        private readonly PlayerBrowserInstanceView _instanceView;

        public PlayerBrowserInstanceHandler(VerticalLayoutGroup worldLayoutGroup, InstanceInfoBase instanceInfo)
        {
            GameObject instanceUI = CommonUtils.SpawnUIPanelFromResourcesAndMoveToParent("PlayerBrowserInstanceInfo", worldLayoutGroup.transform);
            _instanceView = instanceUI.GetComponent<PlayerBrowserInstanceView>();

            _instanceView.Setup(instanceInfo.InstanceCode);
            UpdateInstanceInfo(instanceInfo);
            _instanceView.OnInstanceButtonClicked += () => OnInstanceButtonClicked?.Invoke(instanceInfo.InstanceCode);
        }

        public void UpdateInstanceInfo(InstanceInfoBase instanceInfo)
        {
            Dictionary<ushort, ClientInfoBase> updatedPlayerDict = new();
            ushort hostID = ushort.MaxValue;

            if (instanceInfo is PlatformInstanceInfo platformInstanceInfo)
            {
                foreach (KeyValuePair<ushort, PlatformClientInfo> kvp in platformInstanceInfo.ClientInfos)
                    updatedPlayerDict[kvp.Key] = kvp.Value;
            }
            else if (instanceInfo is InstancedInstanceInfo instancedInstanceInfo)
            {
                foreach (KeyValuePair<ushort, InstancedClientInfo> kvp in instancedInstanceInfo.ClientInfos)
                    updatedPlayerDict[kvp.Key] = kvp.Value;

                //If we're in the instance, we know who the host is, otherwise, we don't, so leave it at ushort.MaxValue
                hostID = instancedInstanceInfo.HostID;
            }

            //Create/update players
            foreach (KeyValuePair<ushort, ClientInfoBase> playerKVP in updatedPlayerDict)
            {
                bool isHost = playerKVP.Key == hostID;

                if (_playerHandlers.ContainsKey(playerKVP.Key))
                {
                    // Update existing player handler
                    _playerHandlers[playerKVP.Key].UpdatePlayerInfo(playerKVP.Value, isHost);
                }
                else
                {
                    // Create new player handler
                    PlayerBrowserPlayerHandler newPlayerHandler = new(_instanceView.PlayersLayoutGroup, playerKVP.Value, isHost);
                    _playerHandlers.Add(playerKVP.Key, newPlayerHandler);
                }
            }
        }
        
        public void Destroy()
        {
            GameObject.Destroy(_instanceView.gameObject);
        }
    }
}
