using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ViRSE.FrameworkRuntime;
using ViRSE.Core.Shared;
using ViRSE.Core.VComponents;
using ViRSE.Core;
using static InstanceSyncSerializables;
using static ViRSE.Core.Shared.CoreCommonSerializables;
using System.IO;

namespace ViRSE.Networking
{
    //TODO - are we calling things WorldState or SyncableState???

    public class WorldStateSyncer //TODO, not sure if "Service" is the right term here
    {
        public static WorldStateSyncer Instance;

        private string _localInstanceCode;
        private int _cycleNumber = 0;

        private const int WORLD_STATE_SYNC_INTERVAL_MS = 20;
        public int WorldStateHistoryQueueSize { get; private set; } = 20; //TODO
        //public UnityEvent<int> OnWorldStateHistoryQueueSizeChange { get; private set; } = new();

        private class SyncInfo
        {
            public WorldStateTransmissionCounter TransmissionCounter;
            public PredictiveWorldStateHistoryQueue HistoryQueue;
            public IStateModule StateModule;
            public byte[] PreviousState;

            public SyncInfo(WorldStateTransmissionCounter transmissionCounter, PredictiveWorldStateHistoryQueue historyQueue, IStateModule stateModule)
            {
                TransmissionCounter = transmissionCounter;
                HistoryQueue = historyQueue;
                StateModule = stateModule;
                PreviousState = null;
            }
        }

        private Dictionary<string, SyncInfo> syncInfosAgainstIDs = new();
        private List<WorldStateBundle> _incommingWorldStateBundleBuffer = new();

        private InstanceService _instanceService;

        public WorldStateSyncer(InstanceService instanceService) 
        {
            _instanceService = instanceService;
            _instanceService.OnReceiveWorldStateSyncableBundle += HandleReceiveWorldStateBundle;

            foreach (IStateModule stateModule in ViRSECoreServiceLocator.Instance.WorldstateSyncableModules)
                RegisterStateModule(stateModule);

            ViRSECoreServiceLocator.Instance.OnStateModuleRegistered += RegisterStateModule;
            ViRSECoreServiceLocator.Instance.OnStateModuleDeregistered += DerigsterFromSyncer;
        } 

        public void TearDown() 
        {
            _instanceService.OnReceiveWorldStateSyncableBundle -= HandleReceiveWorldStateBundle;
        }

        //Happens if we move between instances of the same plugin
        //If changing plugins, this whole syncer will be destroyed and recreated
        public void ResetForNewInstance()
        {
            //May need to tell all the syncables to wipe their data here
        }

        public void RegisterStateModule(IStateModule stateModule)
        {

            WorldStateTransmissionCounter transmissionCounter = new(stateModule.TransmissionFrequency);
            PredictiveWorldStateHistoryQueue historyQueue = new(10); //TODO - need to wire this into the ping, we should probably let all these classes see this limit directly... so a static data?
            SyncInfo syncInfo = new(transmissionCounter, historyQueue, stateModule);

            syncInfosAgainstIDs.Add(stateModule.ID, syncInfo);
        }

        public void DerigsterFromSyncer(IStateModule stateModule)
        {
            syncInfosAgainstIDs.Remove(stateModule.ID);
        }

        public void HandleReceiveWorldStateBundle(byte[] byteData)
        {
            WorldStateBundle worldStateBundle = new(byteData);
            //Debug.Log("Rec state in syncer " + worldStateBundle.WorldStateWrappers.Count);

            _incommingWorldStateBundleBuffer.Add(worldStateBundle);
        }

        public void NetworkUpdate() //TODO, manage buffer size 
        {
            if (!_instanceService.IsConnectedToServer)
                return;

            _cycleNumber++;

            CheckForDestroyedSyncables();
            ProcessReceivedWorldStates();
            TransmitLocalWorldStates();
        }

        private void CheckForDestroyedSyncables()
        {
            //Need a new way around this! Maybe expose GO in IStateModule?
            foreach (string id in syncInfosAgainstIDs.Where(pair => pair.Value == null).Select(pair => pair.Key).ToList())
                syncInfosAgainstIDs.Remove(id);
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
                        if (syncInfosAgainstIDs.TryGetValue(worldStateWrapper.ID, out SyncInfo syncInfo))
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
                    Debug.Log("<color=blue>incomming bundle num states = " + bundle.WorldStateWrappers.Count + "</color>");
                }

            }
        }

        private void TransmitLocalWorldStates()
        {
            List<WorldStateWrapper> outgoingSyncableStateBufferTCP = new();
            List<WorldStateWrapper> outgoingSyncableStateBufferUDP = new();

            foreach (KeyValuePair<string, SyncInfo> pair in syncInfosAgainstIDs)
            {
                SyncInfo syncInfo = pair.Value;

                byte[] newState = syncInfo.StateModule.StateAsBytes;

                bool broadcastFromHost = _instanceService.IsHost && syncInfo.TransmissionCounter.IsOnBroadcastFrame(_cycleNumber);
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

                syncInfo.PreviousState = newState; //Should this be somewhere else?
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

            foreach (KeyValuePair<string, SyncInfo> pair in syncInfosAgainstIDs)
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
    }
}