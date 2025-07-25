using System.Collections.Generic;
using UnityEngine;
using VE2.Common.API;
using VE2.Core.UI.API;
using VE2.NonCore.Instancing.API;
using VE2.NonCore.Platform.API;
using static VE2.NonCore.Instancing.API.InstancePublicSerializables;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.Platform.Internal
{
    //NOTE: Will need to be in the hub to test this, as only in the hub do we connect to the live platform, debug platform wont give us any instances.
    //But then we won't have the instancing??
    //Ok, so start in hub, and then go into an instance, where we'll get both
    //TODO: maybe this should a "controller" or "manager" rather than handler? Since it talks to external services?
    internal class PlayerBrowserHandler
    {
        private readonly IInstanceServiceInternal _instanceServiceInternal;
        private readonly IPlatformServiceInternal _platformServiceInternal;

        private readonly PlayerBrowserView _playerBrowserView;

        private PlayerBrowserWorldHandler _localWorldHandler;
        private Dictionary<string, PlayerBrowserWorldHandler> _remoteWorldHandlers = new();
        //private List<PlayerBrowserWorldInfo> _worldInfos = new();

        public PlayerBrowserHandler()
        {
            _instanceServiceInternal = (IInstanceServiceInternal)VE2API.InstanceService;
            _platformServiceInternal = (IPlatformServiceInternal)VE2API.PlatformService;
            IPrimaryUIService primaryUIService = VE2API.PrimaryUIService;

            // if (_instanceServiceInternal != null)
            //     _instanceServiceInternal.OnInstanceInfoChanged += HandleLocalInstanceUpdated;
            // if (_platformServiceInternal != null)
            //     _platformServiceInternal.OnInstanceInfosChanged += HandleAllInstancesUpdated;

            //Instantiates the prefab, adds it as a tab to the primary UI service, and destroys the holder.
            GameObject playerBrowserUIHolder = GameObject.Instantiate(Resources.Load<GameObject>("PlayerBrowserUIHolder"));
            GameObject playerBrowserUI = playerBrowserUIHolder.transform.GetChild(0).gameObject;
            playerBrowserUI.SetActive(false);

            primaryUIService.AddNewTab("Players", playerBrowserUI, Resources.Load<Sprite>("PlayerBrowserUIIcon"), 1);
            GameObject.Destroy(playerBrowserUIHolder);

            _playerBrowserView = playerBrowserUI.GetComponent<PlayerBrowserView>();
            _playerBrowserView.OnRefreshButtonClicked += UpdateWorldList;
        }

        // private void HandleLocalInstanceUpdated(InstancedInstanceInfo instancedInstanceInfo) => UpdateWorldList();

        // private void HandleAllInstancesUpdated(Dictionary<InstanceCode, PlatformInstanceInfo> platformInstanceInfos) => UpdateWorldList();

        private void UpdateWorldList()
        {
            //Create an up to date list of worlds===============================
            PlayerBrowserWorldInfo localWorldInfo = new PlayerBrowserWorldInfo
            {
                WorldName = _platformServiceInternal.CurrentWorldName,
                LocalInstanceInfo = _instanceServiceInternal.InstanceInfo
            };
            Dictionary<string, PlayerBrowserWorldInfo> newRemoteWorldInfos = new();

            InstanceCode localInstanceCode = _platformServiceInternal.CurrentInstanceCode;

            foreach (KeyValuePair<InstanceCode, PlatformInstanceInfo> kvp in _platformServiceInternal.InstanceInfos)
            {
                if (kvp.Key == localInstanceCode)
                    continue; // Skip the current instance as it's already handled above 

                if (kvp.Key.WorldName == localInstanceCode.WorldName)
                {
                    localWorldInfo.RemoteInstanceInfo.Add(kvp.Key, kvp.Value);
                }
                else
                {
                    if (!newRemoteWorldInfos.ContainsKey(kvp.Key.WorldName))
                    {
                        newRemoteWorldInfos[kvp.Key.WorldName] = new PlayerBrowserWorldInfo
                        {
                            WorldName = kvp.Key.WorldName,
                            LocalInstanceInfo = null
                        };
                    }

                    newRemoteWorldInfos[kvp.Key.WorldName].RemoteInstanceInfo.Add(kvp.Key, kvp.Value);
                }
            }

            //Create/update worlds===============================
            if (_localWorldHandler != null)
                _localWorldHandler.UpdateWorldInfo(localWorldInfo);
            else
                _localWorldHandler = new PlayerBrowserWorldHandler(_playerBrowserView.WorldLayoutGroup, localWorldInfo);

            foreach (KeyValuePair<string, PlayerBrowserWorldInfo> newKVP in newRemoteWorldInfos)
            {
                if (_remoteWorldHandlers.ContainsKey(newKVP.Key))
                {
                    // Update existing world handler
                    _remoteWorldHandlers[newKVP.Key].UpdateWorldInfo(newKVP.Value);
                }
                else
                {
                    // Create new world handler
                    PlayerBrowserWorldHandler newWorldHandler = new(_playerBrowserView.WorldLayoutGroup, newKVP.Value);
                    _remoteWorldHandlers.Add(newKVP.Key, newWorldHandler);
                }
            }

            //Remove worlds that no longer exist===============================
            foreach (KeyValuePair<string, PlayerBrowserWorldHandler> existingKVP in _remoteWorldHandlers)
            {
                if (!newRemoteWorldInfos.ContainsKey(existingKVP.Key))
                {
                    existingKVP.Value.Destroy();
                    _remoteWorldHandlers.Remove(existingKVP.Key);
                }
            }
        }
    }

    internal class PlayerBrowserWorldInfo
    {
        public string WorldName { get; set; }

        public InstancedInstanceInfo LocalInstanceInfo { get; set; } //May be null if local instance is not in this world.
        public Dictionary<InstanceCode, PlatformInstanceInfo> RemoteInstanceInfo { get; set; } = new();
    }
}
