using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace VE2.NonCore.Instancing.Internal
{
    [BurstCompile]
    internal class PredictiveWorldStateHistoryQueue : IDisposable
    {
        private NativeList<byte> flatBuffer;
        private NativeList<int> stateOffsets; // Offset into flatBuffer
        private NativeList<int> stateLengths;
        private int limit;

        public int Count => stateLengths.Length;
        public int Limit
        {
            get => limit;
            set
            {
                if (value != limit)
                    Resize(value);
            }
        }

        public PredictiveWorldStateHistoryQueue(int queueSize)
        {
            if (queueSize <= 0)
                throw new ArgumentException("Queue size must be > 0");

            limit = queueSize;
            flatBuffer = new NativeList<byte>(Allocator.Persistent);
            stateOffsets = new NativeList<int>(limit, Allocator.Persistent);
            stateLengths = new NativeList<int>(limit, Allocator.Persistent);
        }

        private void Resize(int newSize)
        {
            if (newSize == limit) return;

            // Drop oldest states if reducing size
            if (newSize < stateLengths.Length)
            {
                int dropCount = stateLengths.Length - newSize;
                int newStartOffset = stateOffsets[dropCount];
                int newFlatSize = flatBuffer.Length - newStartOffset;

                // Compact the buffer
                NativeArray<byte>.Copy(flatBuffer.AsArray().GetSubArray(newStartOffset, newFlatSize), flatBuffer.AsArray(), newFlatSize);
                flatBuffer.Resize(newFlatSize, NativeArrayOptions.ClearMemory);

                // Shift offsets and lengths
                var newOffsets = new NativeList<int>(newSize, Allocator.Persistent);
                var newLengths = new NativeList<int>(newSize, Allocator.Persistent);
                for (int i = dropCount; i < stateOffsets.Length; i++)
                {
                    newOffsets.Add(stateOffsets[i] - newStartOffset);
                    newLengths.Add(stateLengths[i]);
                }

                stateOffsets.Dispose();
                stateLengths.Dispose();
                stateOffsets = newOffsets;
                stateLengths = newLengths;
            }

            limit = newSize;
        }

        public void AddStateToQueue(byte[] state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (state.Length == 0)
                return;

            if (stateLengths.Length == limit)
            {
                // Remove the oldest state
                int removeLength = stateLengths[0];
                int nextStart = stateOffsets[1];

                // Shift buffer left
                NativeArray<byte>.Copy(flatBuffer.AsArray().GetSubArray(nextStart, flatBuffer.Length - nextStart), flatBuffer.AsArray(), flatBuffer.Length - nextStart);
                flatBuffer.Resize(flatBuffer.Length - removeLength, NativeArrayOptions.ClearMemory);

                // Update offsets
                for (int i = 1; i < stateOffsets.Length; i++)
                    stateOffsets[i - 1] = stateOffsets[i] - removeLength;

                stateOffsets[stateOffsets.Length - 1] = 0; // Will be replaced
                for (int i = 1; i < stateLengths.Length; i++)
                    stateLengths[i - 1] = stateLengths[i];

                stateOffsets.Resize(stateOffsets.Length - 1, NativeArrayOptions.UninitializedMemory);
                stateLengths.Resize(stateLengths.Length - 1, NativeArrayOptions.UninitializedMemory);
            }

            int offset = flatBuffer.Length;
            using (var nativeState = new NativeArray<byte>(state, Allocator.Temp))
                flatBuffer.AddRange(nativeState);

            stateOffsets.Add(offset);
            stateLengths.Add(state.Length);
        }

        public bool DoesStateAppearInStateList(byte[] bytesToCheck)
        {
            if (bytesToCheck == null)
                throw new ArgumentNullException(nameof(bytesToCheck));
            if (Count == 0)
                return false;

            var targetState = new NativeArray<byte>(bytesToCheck, Allocator.TempJob);
            var offsets = new NativeArray<int>(stateOffsets.AsArray(), Allocator.TempJob);
            var lengths = new NativeArray<int>(stateLengths.AsArray(), Allocator.TempJob);
            var flat = new NativeArray<byte>(flatBuffer.AsArray(), Allocator.TempJob);
            var results = new NativeArray<bool>(Count, Allocator.TempJob);

            var job = new CompareFlatStatesJob
            {
                TargetState = targetState,
                StateOffsets = offsets,
                StateLengths = lengths,
                FlatBuffer = flat,
                Result = results
            };

            job.Schedule().Complete();

            bool found = false;
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i])
                {
                    found = true;
                    break;
                }
            }

            results.Dispose();
            flat.Dispose();
            offsets.Dispose();
            lengths.Dispose();
            targetState.Dispose();

            return found;
        }

        public void Dispose()
        {
            if (flatBuffer.IsCreated)
                flatBuffer.Dispose();
            if (stateOffsets.IsCreated)
                stateOffsets.Dispose();
            if (stateLengths.IsCreated)
                stateLengths.Dispose();
        }

        [BurstCompile]
        private struct CompareFlatStatesJob : IJob
        {
            [ReadOnly] public NativeArray<byte> TargetState;
            [ReadOnly] public NativeArray<int> StateOffsets;
            [ReadOnly] public NativeArray<int> StateLengths;
            [ReadOnly] public NativeArray<byte> FlatBuffer;

            [WriteOnly] public NativeArray<bool> Result;

            public void Execute()
            {
                for (int i = 0; i < StateOffsets.Length; i++)
                {
                    if (StateLengths[i] != TargetState.Length)
                    {
                        Result[i] = false;
                        continue;
                    }

                    int offset = StateOffsets[i];
                    bool match = true;
                    for (int j = 0; j < TargetState.Length; j++)
                    {
                        if (FlatBuffer[offset + j] != TargetState[j])
                        {
                            match = false;
                            break;
                        }
                    }

                    Result[i] = match;
                }
            }
        }
    }
}
