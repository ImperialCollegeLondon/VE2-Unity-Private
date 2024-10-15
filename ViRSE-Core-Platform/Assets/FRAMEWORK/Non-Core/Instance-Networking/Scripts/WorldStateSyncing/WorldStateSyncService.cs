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
            public TransmissionProtocol TransmissionProtocol;

            public SyncInfo(WorldStateTransmissionCounter transmissionCounter, PredictiveWorldStateHistoryQueue historyQueue, IStateModule stateModule, byte[] previousState, TransmissionProtocol transmissionProtocol)
            {
                TransmissionCounter = transmissionCounter;
                HistoryQueue = historyQueue;
                StateModule = stateModule;
                PreviousState = previousState;
                TransmissionProtocol = transmissionProtocol;
            }
        }

        private Dictionary<string, SyncInfo> syncInfosAgainstIDs = new();
        private List<WorldStateBundle> _incommingWorldStateBundleBuffer = new();

        private InstanceService _instanceService;

        public WorldStateSyncer(InstanceService instanceService) 
        {
            _instanceService = instanceService;
            _instanceService.OnReceiveWorldStateSyncableBundle += HandleReceiveWorldStateBundle;

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

        public void RegisterStateModule(IStateModule stateModule, string goName, string syncType)
        {
            //Debug.Log("Reg with syncer - " + goName);

            string id = syncType + ":" + goName;
            
            WorldStateTransmissionCounter transmissionCounter = new(stateModule.TransmissionFrequency);
            PredictiveWorldStateHistoryQueue historyQueue = new(10); //TODO - need to wire this into the ping, we should probably let all these classes see this limit directly... so a static data?
            SyncInfo syncInfo = new(transmissionCounter, historyQueue, stateModule, null, stateModule.TransmissionProtocol);

            syncInfosAgainstIDs.Add(id, syncInfo);
        }

        public void DerigsterFromSyncer(string id)
        {
            syncInfosAgainstIDs.Remove(id);
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
            ProcessReceivedWorldStates(_instanceService.IsHost);

            (byte[], byte[]) worldStateBundlesToTransmit = CollectWorldStates(_instanceService.IsHost);
                if (worldStateBundlesToTransmit.Item1 != null)
                    _instanceService.SendWorldStateBundle(worldStateBundlesToTransmit.Item1, TransmissionProtocol.TCP);
                if (worldStateBundlesToTransmit.Item2 != null)
                _instanceService.SendWorldStateBundle(worldStateBundlesToTransmit.Item2, TransmissionProtocol.UDP);
        }

        private void CheckForDestroyedSyncables()
        {
            //Need a new way around this! Maybe expose GO in IStateModule?
            foreach (string id in syncInfosAgainstIDs.Where(pair => pair.Value == null).Select(pair => pair.Key).ToList())
                syncInfosAgainstIDs.Remove(id);
        }

        private void ProcessReceivedWorldStates(bool isHost)
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
                            if (isHost || !syncInfo.HistoryQueue.DoesStateAppearInStateList(worldStateWrapper.StateBytes))
                            {
                                syncInfo.StateModule.StateAsBytes = worldStateWrapper.StateBytes;

                                //If we're not the host, we want to make sure this state doesn't get broadcasted back out
                                if (!isHost)
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

        private (byte[], byte[]) CollectWorldStates(bool isHost)
        {
            List<WorldStateWrapper> outgoingSyncableStateBufferTCP = new();
            List<WorldStateWrapper> outgoingSyncableStateBufferUDP = new();

            foreach (KeyValuePair<string, SyncInfo> pair in syncInfosAgainstIDs)
            {
                SyncInfo syncInfo = pair.Value;

                byte[] newState = syncInfo.StateModule.StateAsBytes;

                bool broadcastFromHost = isHost && syncInfo.TransmissionCounter.IsOnBroadcastFrame(_cycleNumber);

                //if (isHost)
                //    Debug.Log("We host, broadcast frame? " + syncInfo.TransmissionCounter.IsOnBroadcastFrame(_cycleNumber));

                bool transmitFromLocalStateChange = syncInfo.PreviousState != null && !newState.SequenceEqual(syncInfo.PreviousState);

                bool shouldTransmit = broadcastFromHost || transmitFromLocalStateChange;

                if (shouldTransmit)
                {
                    WorldStateWrapper worldStateWrapper = new(pair.Key, newState);
                    //Debug.Log("Should transmit " + pair.Key + " - " + broadcastFromHost + " - " + transmitFromLocalStateChange);

                    if (syncInfo.TransmissionProtocol == TransmissionProtocol.TCP)
                        outgoingSyncableStateBufferTCP.Add(worldStateWrapper);
                    else
                        outgoingSyncableStateBufferUDP.Add(worldStateWrapper);
                }

                syncInfo.PreviousState = newState; //Should this be somewhere else?
                syncInfo.HistoryQueue.AddStateToQueue(newState);
            }

            //Debug.Log("<color=green>Transmit world state " + outgoingSyncableStateBufferTCP.Count + " - " + outgoingSyncableStateBufferUDP.Count + " host: " + isHost + " regs: " + syncInfosAgainstIDs.Count + "</color>");
            byte[] tcpBytes = null;
            byte[] udpBytes = null;

            if (outgoingSyncableStateBufferTCP.Count > 0)
            {
                WorldStateBundle TCPBundle = new(outgoingSyncableStateBufferTCP);
                tcpBytes = TCPBundle.Bytes;
            }

            if (outgoingSyncableStateBufferUDP.Count > 0)
            {
                WorldStateBundle UDPBundle = new(outgoingSyncableStateBufferUDP);
                udpBytes = UDPBundle.Bytes;
            }

            return (tcpBytes, udpBytes);    
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

    // public class WorldStateWrapper : ViRSESerializable
    // {
    //     public string ID { get; private set; }
    //     public byte[] StateBytes { get; private set; }

    //     public WorldStateWrapper(byte[] bytes) : base(bytes) { }

    //     public WorldStateWrapper(string id, byte[] state)
    //     {
    //         ID = id;
    //         StateBytes = state;
    //     }

    //     protected override byte[] ConvertToBytes()
    //     {
    //         using MemoryStream stream = new MemoryStream();
    //         using BinaryWriter writer = new BinaryWriter(stream);

    //         writer.Write(ID);
    //         writer.Write((ushort)StateBytes.Length);
    //         writer.Write(StateBytes);

    //         return stream.ToArray();
    //     }

    //     protected override void PopulateFromBytes(byte[] bytes)
    //     {
    //         using MemoryStream stream = new(bytes);
    //         using BinaryReader reader = new(stream);

    //         ID = reader.ReadString();

    //         int stateBytesLength = reader.ReadUInt16();
    //         StateBytes = reader.ReadBytes(stateBytesLength);
    //     }
    // }
}


/*
 *TODO - snapshopts be handled like this
public class DataCollector : MonoBehaviour
{
    public delegate string NeedListHandler();
    public event NeedListHandler OnNeedList;

    private List<string> collectedData = new List<string>();

    private void Start()
    {
        // Optionally, you can trigger the collection at the start or any other time
        CollectData();
    }

    public void CollectData()
    {
        collectedData.Clear(); // Clear previous data

        if (OnNeedList != null)
        {
            foreach (NeedListHandler handler in OnNeedList.GetInvocationList())
            {
                string data = handler.Invoke();
                collectedData.Add(data);
            }
        }

        // Process collected data
        foreach (var data in collectedData)
        {
            Debug.Log("Collected Data: " + data);
        }
    }
}
*/