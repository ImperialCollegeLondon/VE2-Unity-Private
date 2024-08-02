using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//TODO - should be a MonoBehaviour within PluginService

namespace ViRSE
{
    public static class InstanceSyncService
    {
        public static bool IsHost { get; private set; }
        public static int WorldStateHistoryQueueSize { get; private set; }
        public static UnityEvent<int> OnWorldStateHistoryQueueSizeChange { get; private set; } = new();



        public static void ReceivePingFromHost()
        {
            OnWorldStateHistoryQueueSizeChange.Invoke(5);
        }
    }
}
