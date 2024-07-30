using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

//Has state history check
[Serializable]
public class WorldStateSyncablePredictiveWrapper
{
    private GameObject gameObject;

    [SerializeField, ShowInInspector, HideLabel]
    private WorldstateSyncableModule worldstateSyncableModule; 

    private PredictiveWorldStateHistoryQueue historyQueue;

    //TODO - does this live in the actual state object? 
    //Or does this live here, and the VC doesn't worry about the state change number?
    //[HideInInspector]
    //public int stateChangeNumber = 0;

    [HideInInspector]
    public SyncableStateReceiveEvent ReceivedStateWithNoHistoryMatch {
        get; private set;
    } = new();

    public WorldStateSyncablePredictiveWrapper(GameObject gameObject, string syncType)
    {
        worldstateSyncableModule = new(gameObject, syncType);
    }

    private void Awake()
    {
        ReceivedStateWithNoHistoryMatch = new();
        worldstateSyncableModule.RegisterWithSyncerAndGetSyncableStateReceiveEvent().AddListener(OnReceiveSyncData);
    }

    public string Id { get { return worldstateSyncableModule.id; } }


    //If we're the non-host, the syncable module will have its state updated automatically
    //If we're the host, the VC itself needs to decide whether to accept the received new state
    public void UpdateAndTransmitState(BaseSyncableState state)
    {
        worldstateSyncableModule.UpdateSyncableData(state, true);
    }

    private void FixedUpdate()
    {
        historyQueue.AddStateToQueue((NonPhysicsSyncableState)worldstateSyncableModule.syncableState);
    }

    private void OnReceiveSyncData(BaseSyncableState receivedStateBase)
    {
        //Debug.Log("Receive state " + gameObject.name);
        NonPhysicsSyncableState receivedState = (NonPhysicsSyncableState)receivedStateBase;

        //Only check history if we're non host
        if (!InstanceSyncService.IsHost && historyQueue.DoesStateAppearInRecentStates(receivedState))
            return;

        try
        {
            NonPhysicsSyncableState currentState = (NonPhysicsSyncableState)worldstateSyncableModule.syncableState;

            //Only immediate check if we're non host - non hosts don't broadcast state repeatedly like the host does!
            if (!InstanceSyncService.IsHost && (currentState != null && currentState.CompareState(receivedState)))
            {
                currentState.stateChangeNumber = receivedState.stateChangeNumber;
                return;
            }
        }
        catch (Exception e)
        {
            V_Logger.Error("Error doing immediate state check for syncable on " + gameObject.name + " - " + e.Message, e.StackTrace);
        }

        if (!InstanceSyncService.IsHost)
        {
            worldstateSyncableModule.UpdateSyncableData(receivedState, false);
        }


        ReceivedStateWithNoHistoryMatch?.Invoke(receivedState);
    }
}

public class PredictiveWorldStateHistoryQueue
{
    private FixedSizedQueue<NonPhysicsSyncableState> recentStates;

    public PredictiveWorldStateHistoryQueue()
    {
        recentStates = new();
        recentStates.Limit = InstanceSyncService.WorldStateHistoryQueueSize;

        InstanceSyncService.OnWorldStateHistoryQueueSizeChange.AddListener(HandleWorldStateBufferQueueSizeChange);
    }

    private void HandleWorldStateBufferQueueSizeChange(int newSize)
    {
        recentStates.Limit = newSize;   
    }

    public void AddStateToQueue(NonPhysicsSyncableState state)
    {
        recentStates.Enqueue(state);    
    }

    public bool DoesStateAppearInRecentStates(NonPhysicsSyncableState receivedState)
    {
        foreach (NonPhysicsSyncableState syncableState in recentStates.values)
        {
            //especially for pluginSyncables that haven't yet received data, their history buffers will be full of nulls!
            //Shouldn't ever actually receive a null state, but lets be safe anyway!
            if (receivedState == null || syncableState == null)
            {
                if (receivedState == null && syncableState == null)
                    return true;
                else
                    continue;
            }

            if (syncableState.CompareState(receivedState) && syncableState.CompareStateChangeNumber(receivedState))
                return true;
        }

        return false;
    }
}

public class FixedSizedQueue<T>
{
    ConcurrentQueue<T> q = new();
    private object lockObject = new();

    public int Limit { get; set; }
    public void Enqueue(T obj)
    {
        q.Enqueue(obj);
        lock (lockObject)
        {
            T overflow;
            while (q.Count > Limit && q.TryDequeue(out overflow)) ;
        }
    }

    public int Count => q.Count;

    public T[] values => q.ToArray();

    public void Clear()
    {
        q.Clear();
    }

    public T PeekFront()
    {
        q.TryPeek(out T value);
        return value;
    }
}
