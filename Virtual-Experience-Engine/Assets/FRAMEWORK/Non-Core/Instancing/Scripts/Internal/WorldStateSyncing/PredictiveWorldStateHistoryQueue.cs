using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace VE2.NonCore.Instancing.Internal
{
    [BurstCompile]
    internal class PredictiveWorldStateHistoryQueue
    {
        public FixedSizedQueue<byte[]> RecentStates { get; private set; }

        public PredictiveWorldStateHistoryQueue(int queueSize)
        {
            RecentStates = new()
            {
                Limit = queueSize
            };
        }

        public bool DoesStateAppearInStateList(byte[] bytesToCheck)
        {
            var receivedStateNativeArray = new NativeArray<byte>(bytesToCheck, Allocator.TempJob);
            var jobHandles = new NativeArray<JobHandle>(RecentStates.Count, Allocator.TempJob);
            var results = new NativeArray<NativeArray<bool>>(RecentStates.Count, Allocator.TempJob);
            var recentStateNativeArrays = new NativeArray<byte>[RecentStates.Count]; // Track the NativeArrays to dispose later

            for (int i = 0; i < RecentStates.Count; i++)
            {
                byte[] stateToCheckAgainst = RecentStates.values[i];
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

            //Debug.Log("State appears? " + doesAppear);
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

        private void UpdateWorldStateBufferQueueSize(int newSize)
        {
            RecentStates.Limit = newSize;
        }

        public void AddStateToQueue(byte[] state)
        {
            RecentStates.Enqueue(state);
        }
    }

    internal class FixedSizedQueue<T>
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
}
