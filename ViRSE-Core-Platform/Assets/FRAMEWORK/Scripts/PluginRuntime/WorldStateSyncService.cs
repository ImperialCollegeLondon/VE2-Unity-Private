using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using ViRSE.FrameworkRuntime;

namespace ViRSE.PluginRuntime
{
    //TODO - are we calling things WorldState or SyncableState???

    public class SyncableStateReceiveEvent : UnityEvent<byte[]> { };
    public class OnCollectSnapshotEvent : UnityEvent { };

    public class WorldStateSyncer : MonoBehaviour
    {
        public static WorldStateSyncer Instance;

        private IPluginSyncCommsHandler _commsHandler;
        private string _localInstanceCode;

        [ReadOnly][SerializeField] private int _numberOfSyncablesRegisteredDebug = 0;
        //[SerializeField] private bool printNonHostTransmissionData = false;

        public delegate void SyncableStateReceiver(byte[] newState);
        private Dictionary<string, SyncableStateReceiveEvent> _syncableStateReceivedEvents = new();

        //A delegate where everybody returns a value would make more sence for snapshots
        public delegate void CollectSnapshotReceiver();
        private Dictionary<string, OnCollectSnapshotEvent> _collectSnapshotEvents = new();

        private List<byte[]> _outgoingSyncableStateBufferTCP = new();
        private List<byte[]> _outgoingSyncableStateBufferUDP = new();
        private List<byte[]> _outgoingSyncableStateBufferSnapshot = new();
        private List<byte[]> _incommingWorldStateBundleBuffer = new(); 

        public void Initialize(IPluginSyncCommsHandler commsHandler, string instanceCode)
        {
            _commsHandler = commsHandler;
            _localInstanceCode = instanceCode;
        }

        private void Start()
        {
            _commsHandler.OnWorldStateBundleBytesReceived += HandleReceiveWorldStateBundle;
        }

        private void OnDestroy()
        {
            _commsHandler.OnWorldStateBundleBytesReceived -= HandleReceiveWorldStateBundle;
        }

        public NetworkEvents RegisterForNetworkEvents(string id)
        {
            SyncableStateReceiveEvent syncableStateReceivedEvent = new();
            OnCollectSnapshotEvent collectSnapshotEvent = new();

            _numberOfSyncablesRegisteredDebug++;

            _syncableStateReceivedEvents.Add(id, syncableStateReceivedEvent);
            _collectSnapshotEvents.Add(id, collectSnapshotEvent);

            return new NetworkEvents(syncableStateReceivedEvent, collectSnapshotEvent);
        }

        public void RegisterSnapshotEvents(string id, OnCollectSnapshotEvent collectSnapshotEvent)
        {
            _collectSnapshotEvents.Add(id, collectSnapshotEvent);
            Debug.Log($"Added a snapshot event to CollectSnapshot events  and now the count is {_collectSnapshotEvents.Count}");
        }
        public void DeregisterListener(string id)
        {
            _numberOfSyncablesRegisteredDebug--;

            _syncableStateReceivedEvents.Remove(id);
            _collectSnapshotEvents.Remove(id);
        }

        public void AddStateToOutgoingBuffer(byte[] byteArray, TransmissionProtocol protocol)
        {
            if (_commsHandler.IsConnectedToServer)
                return; //Network isn't ready!

            if (protocol == TransmissionProtocol.TCP)
                _outgoingSyncableStateBufferTCP.Add(byteArray);
            else
                _outgoingSyncableStateBufferUDP.Add(byteArray);
        }

        public void AddWorldStateSnapshot(byte[] byteArray)
        {
            _outgoingSyncableStateBufferSnapshot.Add(byteArray);
        }

        private void HandleReceiveWorldStateBundle(byte[] byteData)
        {
            _incommingWorldStateBundleBuffer.Add(byteData);
        }

        public void UpdateWorldState()
        {
            ProcessReceivedWorldStates();

            //TODO - shouldn't need instance code here, this was so we could reuse the bundle for snapshot messages 
            //Snapshot message should be a little different, and should contain the instance code
            WorldStateBundle TCPBundle = new(_localInstanceCode, _outgoingSyncableStateBufferTCP);
            WorldStateBundle UDPBundle = new(_localInstanceCode, _outgoingSyncableStateBufferUDP);

            if (_outgoingSyncableStateBufferTCP.Count > 0)
                _commsHandler.SendWorldStateBundleBytes(TCPBundle.GetWorldStateBundleAsBytes(), TransmissionProtocol.TCP);

            if (_outgoingSyncableStateBufferUDP.Count > 0)
                _commsHandler.SendWorldStateBundleBytes(UDPBundle.GetWorldStateBundleAsBytes(), TransmissionProtocol.UDP);

            _outgoingSyncableStateBufferTCP.Clear();
            _outgoingSyncableStateBufferUDP.Clear();
        }

        private void ProcessReceivedWorldStates()
        {
            foreach (byte[] receivedBundleBytes in _incommingWorldStateBundleBuffer)
            {
                WorldStateBundle receivedBundle = new WorldStateBundle(receivedBundleBytes);

                if (!receivedBundle.instanceCode.Equals(_localInstanceCode))
                {
                    V_Logger.Error("Received a world state bundle for instance " + receivedBundle.instanceCode + " but we are in instance " + _localInstanceCode);
                    continue;
                }

                foreach (byte[] stateAsBytes in receivedBundle.statesAsBytes)
                {
                    //TODO - Repeated code with BaseWorldStateSyncable
                    //The ID will get read AGAIN when it hits the syncable, doesn't really need to
                    //Maybe the BaseWorldStateSyncable SHOULD be a concrete type here?
                    //Or maybe id just shouldn't live within the state object
                    using (MemoryStream stream = new MemoryStream(stateAsBytes))
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        ushort stringLength = reader.ReadUInt16();
                        byte[] stringBytes = reader.ReadBytes(stringLength);
                        string id = System.Text.Encoding.UTF8.GetString(stringBytes);

                        if (_syncableStateReceivedEvents.TryGetValue(id, out SyncableStateReceiveEvent syncableStateReceiveEvent))
                        {
                            try
                            {
                                //Debug.Log("Emit event");
                                syncableStateReceiveEvent.Invoke(stateAsBytes);
                            }
                            catch (System.Exception ex)
                            {
                                V_Logger.Error("Error receiving syncable data - " + ex.StackTrace + " - " + ex.Message);
                            }
                        }
                    }
                }
            }

            _incommingWorldStateBundleBuffer.Clear();
        }

        public void CollectAndTransmitWorldStateSnapshot(string instanceCodeOfSnapshot)
        {
            foreach (KeyValuePair<string, OnCollectSnapshotEvent> snapshotEvent in _collectSnapshotEvents)
            {
                try
                {
                    snapshotEvent.Value.Invoke();
                    //Debug.Log("Got a snapShot from tryGetValue");
                }
                catch (System.Exception ex)
                {
                    V_Logger.Error("Error getting world state snapshots - " + ex.StackTrace + " - " + ex.Message);
                }
            }

            if (_outgoingSyncableStateBufferSnapshot.Count > 0)
            {
                WorldStateBundle worldStateSnapshotBundle = new(instanceCodeOfSnapshot, _outgoingSyncableStateBufferSnapshot);

                _commsHandler.SendTCPWorldStateSnapshotBytes(worldStateSnapshotBundle.GetWorldStateBundleAsBytes());
                _outgoingSyncableStateBufferSnapshot.Clear();
            }

        }
    }

    public class WorldStateBundle
    {
        public string instanceCode;
        public List<byte[]> statesAsBytes { get; private set; } = new();

        public WorldStateBundle(byte[] bundleAsBytes)
        {
            PopulateFromByteData(bundleAsBytes);
        }

        public WorldStateBundle(string instanceCode, List<byte[]> statesAsBytes)
        {
            this.instanceCode = instanceCode;
            this.statesAsBytes = statesAsBytes;
        }

        public byte[] GetWorldStateBundleAsBytes()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                byte[] instanceCodeBytes = System.Text.Encoding.UTF8.GetBytes(instanceCode);
                writer.Write((ushort)instanceCodeBytes.Length);
                writer.Write(instanceCodeBytes);

                foreach (byte[] stateAsBytes in statesAsBytes)
                {
                    writer.Write((ushort)stateAsBytes.Length);
                    writer.Write(stateAsBytes);
                }

                return stream.ToArray();
            }
        }

        public void PopulateFromByteData(byte[] bundleAsBytes)
        {
            using (MemoryStream stream = new MemoryStream(bundleAsBytes))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                int instanceCodeBytesLength = reader.ReadUInt16();
                byte[] instanceCodeBytes = reader.ReadBytes(instanceCodeBytesLength);
                instanceCode = System.Text.Encoding.UTF8.GetString(instanceCodeBytes);

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    int stateBytesLength = reader.ReadUInt16();
                    byte[] stateBytes = reader.ReadBytes(stateBytesLength);
                    statesAsBytes.Add(stateBytes);
                }
            }
        }
    }

    public class NetworkEvents
    {
        public SyncableStateReceiveEvent SyncableStateReceivedEvent { get; private set; }
        public OnCollectSnapshotEvent CollectSnapshotEvent { get; private set; }

        public NetworkEvents(SyncableStateReceiveEvent syncableStateReceivedEvent, OnCollectSnapshotEvent collectSnapshotEvent)
        {
            SyncableStateReceivedEvent = syncableStateReceivedEvent;
            CollectSnapshotEvent = collectSnapshotEvent;
        }
    }
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