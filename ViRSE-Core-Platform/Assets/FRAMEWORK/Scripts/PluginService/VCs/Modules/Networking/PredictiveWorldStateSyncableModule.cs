using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PredictiveWorldStateSyncableModule : WorldstateSyncableModule
{
    private PredictiveWorldStateHistoryQueue _historyQueue = new();
    public SyncableStateReceiveEvent OnReceivedStateWithNoHistoryMatch { get; private set; } = new();

    protected override void FixedUpdate() //TODO - needs to be called by the VC
    {
        base.FixedUpdate();
        _historyQueue.AddStateToQueue((PredictiveSyncableState)_state);
    }

    protected override void OnReceiveStateFromSyncer(BaseSyncableState receivedStateBase)
    {
        base.OnReceiveStateFromSyncer(receivedStateBase); //Emits the standard event

        //Debug.Log("Receive state " + gameObject.name);
        PredictiveSyncableState receivedState = (PredictiveSyncableState)receivedStateBase;

        //Only check history if we're non host
        if (!InstanceSyncService.IsHost && _historyQueue.DoesStateAppearInRecentStates(receivedState))
            return;

        try
        {
            PredictiveSyncableState currentState = (PredictiveSyncableState)_state;

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

        //TODO - the VC should probably do this 
        //if (!InstanceSyncService.IsHost)
        //{
        //    _worldstateSyncableModule.UpdateSyncableData(receivedState, false);
        //}


        OnReceivedStateWithNoHistoryMatch?.Invoke(receivedState); //TODO, inform VC if change comes from host or not
    }
}

public class PredictiveWorldStateHistoryQueue
{
    private FixedSizedQueue<PredictiveSyncableState> recentStates;

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

    public void AddStateToQueue(PredictiveSyncableState state)
    {
        recentStates.Enqueue(state);    
    }

    public bool DoesStateAppearInRecentStates(PredictiveSyncableState receivedState)
    {
        foreach (PredictiveSyncableState syncableState in recentStates.values)
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
