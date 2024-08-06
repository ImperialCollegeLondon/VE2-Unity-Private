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
    /// In charge of all sync management for the instance. Orchestrates WorldStateSyncer, PlayerSyncer, and InstantMessages
    /// </summary>
    public class PluginSyncService : MonoBehaviour
    {
        public static PluginSyncService Instance { get; private set; }  // Do we even want a singleton here???
        //Well, we can't wire the PluginSyncService into the VCs... and the VCs are part of the PluginRuntime anyway

        public bool IsHost { get; private set; }
        public int WorldStateHistoryQueueSize { get; private set; }
        public UnityEvent<int> OnWorldStateHistoryQueueSizeChange { get; private set; } = new();

        private IPrimaryServerService _primaryServerService;
        private WorldStateSyncer _worldStateSyncer;

        public void Initialize(IPrimaryServerService primaryServerService)
        {
            Instance = this;

            _primaryServerService = primaryServerService;

            _worldStateSyncer = gameObject.AddComponent<WorldStateSyncer>();

            //TODO - 
            _worldStateSyncer.Initialize(_primaryServerService);
        }

        public void ReceivePingFromHost()
        {
            OnWorldStateHistoryQueueSizeChange.Invoke(5);
        }
    }
}

/*Flow 
 * PluginRuntime creates a PluginSyncService immediately 
 * PluginSyncService creates a WorldStateSyncer immediately 
 * SyncableModules all check if WorldStateSyncer is ready, if not, they listen to the OnReady event
 * 
 * 
 * 
 * 
 */
