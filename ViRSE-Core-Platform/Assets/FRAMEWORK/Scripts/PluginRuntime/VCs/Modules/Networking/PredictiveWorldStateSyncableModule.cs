using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor.SceneManagement;
using UnityEngine;
using ViRSE.FrameworkRuntime;

namespace ViRSE.PluginRuntime.VComponents
{
    [BurstCompile]
    public class PredictiveWorldStateSyncableModule : WorldstateSyncableModule
    {
        private PredictiveWorldStateHistoryQueue _historyQueue = new();
        public SyncableStateReceiveEvent OnReceivedStateWithNoHistoryMatch { get; private set; } = new();

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            _historyQueue.AddStateToQueue(_state.Bytes);
        }

        protected override void OnReceiveStateFromSyncer(byte[] receivedStateBytes)
        {
            base.OnReceiveStateFromSyncer(receivedStateBytes); //Emits the standard event

            //Debug.Log("Syncable rec state");

            //If our state is null, we already know to override to whatevers coming from the network
            //We only ever SEND if state isn't null, so this must be something new 
            if (_state == null)
            {
                AddReceivedStateToBufferAndInvokeEvent(receivedStateBytes);
            }
            else
            {
                List<byte[]> statesToCheck = new()
                {
                    _state.Bytes
                };

                //if we're not the host, we've "predicting" what we will receive in the future
                //We should check our previous states to see if we've been predicting correctly
                if (!PluginSyncService.Instance.IsHost)
                    statesToCheck.AddRange(_historyQueue.RecentStates.values.ToList());

                if (!DoesStateAppearInStateList(receivedStateBytes, statesToCheck))
                    AddReceivedStateToBufferAndInvokeEvent(receivedStateBytes);
            }
        }

        private void AddReceivedStateToBufferAndInvokeEvent(byte[] stateAsBytes)
        {
            _historyQueue.AddStateToQueue(_state.Bytes);
            OnReceivedStateWithNoHistoryMatch?.Invoke(stateAsBytes);
        }

        private bool DoesStateAppearInStateList(byte[] receivedStateAsBytes, List<byte[]> statesToCheckAgainst)
        {
            var receivedStateNativeArray = new NativeArray<byte>(receivedStateAsBytes, Allocator.TempJob);
            var jobHandles = new NativeArray<JobHandle>(statesToCheckAgainst.Count(), Allocator.TempJob);
            var results = new NativeArray<NativeArray<bool>>(statesToCheckAgainst.Count(), Allocator.TempJob);
            var recentStateNativeArrays = new NativeArray<byte>[statesToCheckAgainst.Count()]; // Track the NativeArrays to dispose later

            for (int i = 0; i < statesToCheckAgainst.Count(); i++)
            {
                byte[] stateToCheckAgainst = statesToCheckAgainst[i];
                if (stateToCheckAgainst == null)
                {
                    jobHandles[i] = default;
                    continue;
                }

                var recentStateNativeArray = new NativeArray<byte>(stateToCheckAgainst, Allocator.TempJob);
                recentStateNativeArrays[i] = recentStateNativeArray; // Keep a reference to dispose later
                var result = new NativeArray<bool>(1, Allocator.TempJob);
                results[i] = result;

                var job = new CompareBytesJob
                {
                    Array1 = recentStateNativeArray,
                    Array2 = receivedStateNativeArray,
                    Result = result
                };

                jobHandles[i] = job.Schedule();
            }

            JobHandle.CompleteAll(jobHandles);

            bool doesAppear = false;
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].IsCreated && results[i][0])
                {
                    doesAppear = true;
                    break;
                }
            }

            receivedStateNativeArray.Dispose();
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].IsCreated)
                    results[i].Dispose();
            }
            for (int i = 0; i < recentStateNativeArrays.Length; i++)
            {
                if (recentStateNativeArrays[i].IsCreated)
                    recentStateNativeArrays[i].Dispose();
            }
            results.Dispose();
            jobHandles.Dispose();

            return doesAppear;
        }

        [BurstCompile]
        private struct CompareBytesJob : IJob
        {
            [Unity.Collections.ReadOnly] public NativeArray<byte> Array1;
            [Unity.Collections.ReadOnly] public NativeArray<byte> Array2;
            [WriteOnly] public NativeArray<bool> Result;

            public void Execute()
            {
                if (Array1.Length != Array2.Length)
                {
                    Result[0] = false;
                    return;
                }

                for (int i = 0; i < Array1.Length; i++)
                {
                    if (Array1[i] != Array2[i])
                    {
                        Result[0] = false;
                        return;
                    }
                }

                Result[0] = true;
            }
        }
    }

    public class PredictiveWorldStateHistoryQueue
    {
        public FixedSizedQueue<byte[]> RecentStates { get; private set; }

        public PredictiveWorldStateHistoryQueue()
        {
            RecentStates = new();
            RecentStates.Limit = PluginSyncService.Instance.WorldStateHistoryQueueSize;

            PluginSyncService.Instance.OnWorldStateHistoryQueueSizeChange.AddListener(HandleWorldStateBufferQueueSizeChange);
        }

        private void HandleWorldStateBufferQueueSizeChange(int newSize)
        {
            RecentStates.Limit = newSize;
        }

        public void AddStateToQueue(byte[] state)
        {
            RecentStates.Enqueue(state);
        }
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
