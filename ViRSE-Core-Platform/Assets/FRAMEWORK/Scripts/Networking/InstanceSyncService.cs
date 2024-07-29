using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class InstanceSyncService
{
    public static bool IsHost { get; private set; }
    public static int WorldStateHistoryQueueSize { get; private set; }
    public static UnityEvent<int> OnWorldStateHistoryQueueSizeChange { get; private set; }



    public static void ReceivePingFromHost()
    {
        OnWorldStateHistoryQueueSizeChange.Invoke(5);
    }
}
