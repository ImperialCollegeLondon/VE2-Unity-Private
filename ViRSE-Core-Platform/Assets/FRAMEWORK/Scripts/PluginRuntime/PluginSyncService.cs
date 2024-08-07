using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using ViRSE.FrameworkRuntime;

namespace ViRSE.PluginRuntime
{
    /// <summary>
    /// In charge of all sync management for the instance. Orchestrates WorldStateSyncer, PlayerSyncers, and InstantMessageRouter 
    /// </summary>
    public class PluginSyncService : MonoBehaviour
    {
        //TODO, should take some config for events like "OnBecomeHost", "OnLoseHost", maybe also sync frequencies

        public static PluginSyncService Instance { get; private set; }  // Do we even want a singleton here???
        //Well, we can't wire the PluginSyncService into the VCs... and the VCs are part of the PluginRuntime anyway

        public bool ReadyToSync => _primaryServerService.ReadyToSyncPlugin;
        public event Action OnReadyToSync;

        public bool IsHost => _primaryServerService.LocalInstanceInfo.HostID == _primaryServerService.LocalClientID;

        public int WorldStateHistoryQueueSize { get; private set; }
        public UnityEvent<int> OnWorldStateHistoryQueueSizeChange { get; private set; } = new();

        private IPrimaryServerService _primaryServerService;
        private WorldStateSyncer _worldStateSyncer;

        //TODO, consider constructing PluginSyncService using factory pattern to inject the WorldStateSyncer
        //Also consider removing thsi from MonoBehvaiour so we get our constructor back
        public void Initialize(IPrimaryServerService primaryServerService)
        {
            Instance = this;

            WorldStateHistoryQueueSize = 10; //TODO, automate this, currently 10 is more than enough though, 200ms

            _primaryServerService = primaryServerService;
            _primaryServerService.OnReadyToSyncPlugin += HandleReadyToSyncPlugin;
            IPluginSyncCommsHandler pluginSyncCommsHandler = _primaryServerService.PluginSyncCommsHandler;

            _worldStateSyncer = gameObject.AddComponent<WorldStateSyncer>();
            _worldStateSyncer.Initialize((IPluginWorldStateCommsHandler)pluginSyncCommsHandler);
        }

        private void HandleReadyToSyncPlugin()
        {
            OnReadyToSync?.Invoke();
        }

        private void FixedUpdate()
        {
            _worldStateSyncer.TickOver();
        }

        public void ReceivePingFromHost()
        {
            //TODO calc buffer size
            _worldStateSyncer.SetNewBufferLength(1);
            OnWorldStateHistoryQueueSizeChange.Invoke(5);
        }
    }
}
