using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using UnityEngine.XR;
using VE2.Core.VComponents.API;
using VE2.Core.Common;
using static VE2.NonCore.Instancing.Internal.InstanceSyncSerializables;

namespace VE2.NonCore.Instancing.Internal
{
    internal class WorldStateSyncer : IWorldStateSyncService
    {
        #region syncer interfaces
        public void RegisterWorldStateModule(IWorldStateModule stateModule)
        {
            PredictiveWorldStateHistoryQueue historyQueue = new(WorldStateHistoryQueueSize); 
            SyncInfo syncInfo = new() { HostSyncOffset = GenerateNewHostSyncOffset(), HistoryQueue = historyQueue, StateModule = stateModule, PreviousState = null };

            _syncInfosAgainstIDs.Add(stateModule.ID, syncInfo);
        }

        public void DeregisterWorldStateModule(IWorldStateModule stateModule)
        {
            if (_syncInfosAgainstIDs.TryGetValue(stateModule.ID, out SyncInfo syncInfo))
            {
                _numOfSyncablesPerSyncOffsets[syncInfo.HostSyncOffset]--;
                _syncInfosAgainstIDs.Remove(stateModule.ID);
            }
        }
        #endregion

        //public event Action<BytesAndProtocol> OnLocalChangeOrHostBroadcastWorldStateData; 

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

        private IPluginSyncCommsHandler _commsHandler;
        //private readonly WorldStateModulesContainer _worldStateModulesContainer; //TODO: remove
        private readonly InstanceInfoContainer _instanceInfoContainer;

        public WorldStateSyncer(IPluginSyncCommsHandler commsHandler, InstanceInfoContainer instanceInfoContainer) 
        {
            _commsHandler = commsHandler;
            _commsHandler.OnReceiveWorldStateSyncableBundle += HandleReceiveWorldStateBundle;

            _instanceInfoContainer = instanceInfoContainer;

            // _worldStateModulesContainer.OnWorldStateModuleRegistered += RegisterStateModule;
            // _worldStateModulesContainer.OnWorldStateModuleDeregistered += DerigsterFromSyncer;

            // foreach (IWorldStateModule stateModule in _worldStateModulesContainer.WorldstateSyncableModules)
            //     RegisterStateModule(stateModule);

            //TODO: what exactly is the lifecycle of this? Destroyed between instances?
            //Maybe even between different connections on the same plugin? 
            //Seems cleaner to just tear everything down and start again when we reconnect?
            //That potentially makes interface access difficult though? 
            //Not really, these modules aren't going to be exposed to the plugin in any way 
            //And if they are, we just have something wired in the interface to just say "if syncer doesn't exit, return empty rather than null"

        } 

        public void HandleReceiveWorldStateBundle(byte[] byteData)
        {
            WorldStateBundle worldStateBundle = new(byteData);
            _incommingWorldStateBundleBuffer.Add(worldStateBundle);
        }

        public void NetworkUpdate() //TODO, manage buffer size 
        {
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
                            if (_instanceInfoContainer.IsHost || !syncInfo.HistoryQueue.DoesStateAppearInStateList(worldStateWrapper.StateBytes))
                            {
                                syncInfo.StateModule.StateAsBytes = worldStateWrapper.StateBytes;

                                //If we're not the host, we want to make sure this state doesn't get broadcasted back out
                                if (!_instanceInfoContainer.IsHost)
                                    syncInfo.PreviousState = worldStateWrapper.StateBytes;
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.Log(e.Message + "\n bundles: " + _incommingWorldStateBundleBuffer.Count + " - " + e.StackTrace);
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

                bool broadcastFromHost = _instanceInfoContainer.IsHost && (_cycleNumber + syncInfo.HostSyncOffset) % syncInfo.HostSyncInterval == 0;
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
                _commsHandler.SendMessage(TCPBundle.Bytes, InstanceSyncSerializables.InstanceNetworkingMessageCodes.WorldstateSyncableBundle, TransmissionProtocol.TCP);
            }

            if (outgoingSyncableStateBufferUDP.Count > 0)
            {
                WorldStateBundle UDPBundle = new(outgoingSyncableStateBufferUDP);
                _commsHandler.SendMessage(UDPBundle.Bytes, InstanceSyncSerializables.InstanceNetworkingMessageCodes.WorldstateSyncableBundle, TransmissionProtocol.UDP);
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

        public void TearDown()
        {
            _commsHandler.OnReceiveWorldStateSyncableBundle -= HandleReceiveWorldStateBundle;

            // _worldStateModulesContainer.OnWorldStateModuleRegistered -= RegisterStateModule;
            // _worldStateModulesContainer.OnWorldStateModuleDeregistered -= DerigsterFromSyncer;

            _numOfSyncablesPerSyncOffsets.Clear();
            _syncInfosAgainstIDs.Clear();
        }
    }
}