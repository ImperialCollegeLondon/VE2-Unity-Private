using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static InstanceSyncSerializables;
using VE2.Common;

namespace VE2.InstanceNetworking
{
    public static class WorldStateSyncerFactory 
    {
        public static WorldStateSyncer Create(InstanceService instanceService) 
        {
            return new WorldStateSyncer(instanceService, VE2CoreServiceLocator.Instance.WorldStateModulesContainer);
        }
    }

    public class WorldStateSyncer 
    {
        private class SyncInfo
        {
            public int HostSyncOffset;
            public PredictiveWorldStateHistoryQueue HistoryQueue;
            public IWorldStateModule StateModule;
            public byte[] PreviousState;

            public int HostSyncInterval => (int)(50 / StateModule.TransmissionFrequency);
        }
        private readonly Dictionary<int, int> _numOfSyncablesPerSyncOffsets = new();

        private readonly Dictionary<string, SyncInfo> _syncInfosAgainstIDs = new();
        private readonly List<WorldStateBundle> _incommingWorldStateBundleBuffer = new();
        private readonly string _localInstanceCode;
        private int _cycleNumber = 0;

        private const int WORLD_STATE_SYNC_INTERVAL_MS = 20;
        public int WorldStateHistoryQueueSize { get; private set; } = 100; //TODO tie this into ping

        private readonly InstanceService _instanceService;
        private readonly WorldStateModulesContainer _worldStateModulesContainer;

        public WorldStateSyncer(InstanceService instanceService, WorldStateModulesContainer worldStateModulesContainer) 
        {
            _instanceService = instanceService;
            _instanceService.OnReceiveWorldStateSyncableBundle += HandleReceiveWorldStateBundle;

            _worldStateModulesContainer = worldStateModulesContainer;
            _worldStateModulesContainer.OnWorldStateModuleRegistered += RegisterStateModule;
            _worldStateModulesContainer.OnWorldStateModuleDeregistered += DerigsterFromSyncer;

            foreach (IWorldStateModule stateModule in worldStateModulesContainer.WorldstateSyncableModules)
                RegisterStateModule(stateModule);
        } 

        public void TearDown() 
        {
            _instanceService.OnReceiveWorldStateSyncableBundle -= HandleReceiveWorldStateBundle;
            _worldStateModulesContainer.OnWorldStateModuleRegistered -= RegisterStateModule;
            _worldStateModulesContainer.OnWorldStateModuleDeregistered -= DerigsterFromSyncer;
        }

        //Happens if we move between instances of the same plugin
        //If changing plugins, this whole syncer will be destroyed and recreated
        public void ResetForNewInstance()
        {
            //May need to tell all the syncables to wipe their data here
        }

        public void RegisterStateModule(IWorldStateModule stateModule)
        {
            PredictiveWorldStateHistoryQueue historyQueue = new(WorldStateHistoryQueueSize); 
            SyncInfo syncInfo = new() { HostSyncOffset = GenerateNewHostSyncOffset(), HistoryQueue = historyQueue, StateModule = stateModule, PreviousState = null };

            _syncInfosAgainstIDs.Add(stateModule.ID, syncInfo);
        }

        public void DerigsterFromSyncer(IWorldStateModule stateModule)
        {
            if (_syncInfosAgainstIDs.TryGetValue(stateModule.ID, out SyncInfo syncInfo))
            {
                _numOfSyncablesPerSyncOffsets[syncInfo.HostSyncOffset]--;
                _syncInfosAgainstIDs.Remove(stateModule.ID);
            }
        }

        public void HandleReceiveWorldStateBundle(byte[] byteData)
        {
            WorldStateBundle worldStateBundle = new(byteData);
            _incommingWorldStateBundleBuffer.Add(worldStateBundle);
        }

        public void NetworkUpdate() //TODO, manage buffer size 
        {
            if (!_instanceService.IsConnectedToServer)
                return;

            _cycleNumber++;

            ProcessReceivedWorldStates();
            TransmitLocalWorldStates();
        }

        private void ProcessReceivedWorldStates()
        {
            try
            {
                List<WorldStateBundle> worldStateBundlesToProcess = new(_incommingWorldStateBundleBuffer);

                _incommingWorldStateBundleBuffer.Clear();

                foreach (WorldStateBundle receivedBundle in worldStateBundlesToProcess)
                {
                    //Debug.Log("<color=blue>receive world state " + receivedBundle.WorldStateWrappers.Count + "</color>");

                    foreach (WorldStateWrapper worldStateWrapper in receivedBundle.WorldStateWrappers)
                    {
                        if (_syncInfosAgainstIDs.TryGetValue(worldStateWrapper.ID, out SyncInfo syncInfo))
                        {
                            //We only do the hitory check if we're not the host
                            if (_instanceService.IsHost || !syncInfo.HistoryQueue.DoesStateAppearInStateList(worldStateWrapper.StateBytes))
                            {
                                syncInfo.StateModule.StateAsBytes = worldStateWrapper.StateBytes;

                                //If we're not the host, we want to make sure this state doesn't get broadcasted back out
                                if (!_instanceService.IsHost)
                                    syncInfo.PreviousState = worldStateWrapper.StateBytes;
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.Log(e.Message + "\n bundles: " + _incommingWorldStateBundleBuffer.Count);
                foreach (var bundle in _incommingWorldStateBundleBuffer)
                {
                    Debug.Log("<color=blue>incomming bundle num states = " + bundle.WorldStateWrappers.Count + "</color> \n" + e.StackTrace);
                }
            }
        }

        private void TransmitLocalWorldStates()
        {
            List<WorldStateWrapper> outgoingSyncableStateBufferTCP = new();
            List<WorldStateWrapper> outgoingSyncableStateBufferUDP = new();

            foreach (KeyValuePair<string, SyncInfo> pair in _syncInfosAgainstIDs)
            {
                SyncInfo syncInfo = pair.Value;
                byte[] newState = syncInfo.StateModule.StateAsBytes;

                bool broadcastFromHost = _instanceService.IsHost && (_cycleNumber + syncInfo.HostSyncOffset) % syncInfo.HostSyncInterval == 0;
                bool transmitFromLocalStateChange = syncInfo.PreviousState != null && !newState.SequenceEqual(syncInfo.PreviousState);
                bool shouldTransmit = broadcastFromHost || transmitFromLocalStateChange;

                if (shouldTransmit)
                {
                    WorldStateWrapper worldStateWrapper = new(pair.Key, newState);

                    if (syncInfo.StateModule.TransmissionProtocol == TransmissionProtocol.TCP)
                        outgoingSyncableStateBufferTCP.Add(worldStateWrapper);
                    else
                        outgoingSyncableStateBufferUDP.Add(worldStateWrapper);
                }

                syncInfo.PreviousState = newState;
                syncInfo.HistoryQueue.AddStateToQueue(newState);
            }

            if (outgoingSyncableStateBufferTCP.Count > 0)
            {
                WorldStateBundle TCPBundle = new(outgoingSyncableStateBufferTCP);
                _instanceService.SendWorldStateBundle(TCPBundle.Bytes, TransmissionProtocol.TCP);
            }

            if (outgoingSyncableStateBufferUDP.Count > 0)
            {
                WorldStateBundle UDPBundle = new(outgoingSyncableStateBufferUDP);
                _instanceService.SendWorldStateBundle(UDPBundle.Bytes, TransmissionProtocol.UDP);
            }
        }

        public void SetNewBufferLength(int newLength)
        {
            //Change length on syncables 
        }

        public byte[] CollectAndTransmitWorldStateSnapshot(string instanceCodeOfSnapshot)
        {
            List<WorldStateWrapper> outgoingSyncableStateBufferSnapshot = new();

            foreach (KeyValuePair<string, SyncInfo> pair in _syncInfosAgainstIDs)
            {
                SyncInfo syncInfo = pair.Value;

                WorldStateWrapper worldStateWrapper = new(pair.Key, syncInfo.StateModule.StateAsBytes);
                outgoingSyncableStateBufferSnapshot.Add(worldStateWrapper);
            }

            if (outgoingSyncableStateBufferSnapshot.Count > 0)
            {
                WorldStateBundle worldStateBundle = new(outgoingSyncableStateBufferSnapshot);
                WorldStateSnapshot worldStateSnapshot = new(instanceCodeOfSnapshot, worldStateBundle);

                return worldStateSnapshot.Bytes;
            }

            return null;
        }

        public int GenerateNewHostSyncOffset()
        {
            //To smooth out the transmission load, choose the least used offset
            int newSyncOffset = 0;
            int timesLeastUsedSyncOffsetUsed = int.MaxValue;

            for (int i = 0; i < 50; i++)
            {
                int timesSyncOffsetUsed = _numOfSyncablesPerSyncOffsets.ContainsKey(i) ?
                    _numOfSyncablesPerSyncOffsets[i] : 0;

                if (timesSyncOffsetUsed < timesLeastUsedSyncOffsetUsed)
                {
                    timesLeastUsedSyncOffsetUsed = timesSyncOffsetUsed;
                    newSyncOffset = i;
                }

                if (timesLeastUsedSyncOffsetUsed == 0)
                    break; //No need to keep searching, can't be less than zero!
            }

            if (_numOfSyncablesPerSyncOffsets.ContainsKey(newSyncOffset))
                _numOfSyncablesPerSyncOffsets[newSyncOffset]++;
            else
                _numOfSyncablesPerSyncOffsets.Add(newSyncOffset, 1);

            return newSyncOffset;
        }
    }
}