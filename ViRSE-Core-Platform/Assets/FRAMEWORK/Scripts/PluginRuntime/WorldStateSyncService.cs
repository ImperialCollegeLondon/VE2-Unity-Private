using Sirenix.OdinInspector;
using System;
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

        private IPluginWorldStateCommsHandler _commsHandler;
        private string _localInstanceCode;

        [ReadOnly][SerializeField] private int _numberOfSyncablesRegisteredDebug = 0;
        //[SerializeField] private bool printNonHostTransmissionData = false;

        public delegate void SyncableStateReceiver(byte[] newState);
        private Dictionary<string, SyncableStateReceiveEvent> _syncableStateReceivedEvents = new();

        //A delegate where everybody returns a value would make more sence for snapshots
        public delegate void CollectSnapshotReceiver();
        private Dictionary<string, OnCollectSnapshotEvent> _collectSnapshotEvents = new();

        private List<WorldStateWrapper> _outgoingSyncableStateBufferTCP = new();
        private List<WorldStateWrapper> _outgoingSyncableStateBufferUDP = new();
        private List<WorldStateWrapper> _outgoingSyncableStateBufferSnapshot = new();
        private List<WorldStateBundle> _incommingWorldStateBundleBuffer = new();

        private event Action<string> _onInstanceCodeChange; //Don't like this

        public void Initialize(IPluginWorldStateCommsHandler commsHandler)
        {
            Instance = this;
            _commsHandler = commsHandler;
        }

        //Happens if we move between instances of the same plugin
        //If changing plugins, this whole syncer will be destroyed and recreated
        public void ResetForNewInstance()
        {
            //May need to tell all the syncables to wipe their data here
        }

        private void Start()
        {
            _commsHandler.OnReceiveWorldStateSyncableBundle += HandleReceiveWorldStateBundle;
        }

        private void OnDestroy()
        {
            _commsHandler.OnReceiveWorldStateSyncableBundle -= HandleReceiveWorldStateBundle;
        }

        public NetworkEvents RegisterForNetworkEvents(string id)
        {
            SyncableStateReceiveEvent syncableStateReceivedEvent = new();
            OnCollectSnapshotEvent collectSnapshotEvent = new();

            _numberOfSyncablesRegisteredDebug++;

            _syncableStateReceivedEvents.Add(id, syncableStateReceivedEvent);
            _collectSnapshotEvents.Add(id, collectSnapshotEvent);

            //Debug.Log("Register " + id);

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

        public void AddStateToOutgoingBuffer(string id, byte[] stateBytes, TransmissionProtocol protocol)
        {
            if (!_commsHandler.IsReadyToTransmit)
                return; //Network isn't ready!

            WorldStateWrapper worldStateWrapper = new(id, stateBytes);

            if (protocol == TransmissionProtocol.TCP)
                _outgoingSyncableStateBufferTCP.Add(worldStateWrapper);
            else
                _outgoingSyncableStateBufferUDP.Add(worldStateWrapper);
        }

        public void AddStateToOutgoingSnapshot(string id, byte[] stateBytes)
        {
            WorldStateWrapper worldStateWrapper = new(id, stateBytes);
            _outgoingSyncableStateBufferSnapshot.Add(worldStateWrapper);
        }

        private void HandleReceiveWorldStateBundle(byte[] byteData)
        {
            //Debug.Log("REc state in syncer");
            WorldStateBundle worldStateBundle = new(byteData);
            _incommingWorldStateBundleBuffer.Add(worldStateBundle);
        }

        public void TickOver()
        {
            ProcessReceivedWorldStates();

            WorldStateBundle TCPBundle = new(_outgoingSyncableStateBufferTCP);
            WorldStateBundle UDPBundle = new(_outgoingSyncableStateBufferUDP);

            if (_outgoingSyncableStateBufferTCP.Count > 0)
                _commsHandler.SendWorldStateBundle(TCPBundle.Bytes, TransmissionProtocol.TCP);

            if (_outgoingSyncableStateBufferUDP.Count > 0)
                _commsHandler.SendWorldStateBundle(UDPBundle.Bytes, TransmissionProtocol.UDP);

            _outgoingSyncableStateBufferTCP.Clear();
            _outgoingSyncableStateBufferUDP.Clear();
        }

        public void SetNewBufferLength(int newLength)
        {
            //Emit event to syncables?
        }

        private void ProcessReceivedWorldStates()
        {
            foreach (WorldStateBundle receivedBundle in _incommingWorldStateBundleBuffer)
            {
                foreach (WorldStateWrapper worldStateWrapper in receivedBundle.WorldStateWrappers)
                {
                    if (_syncableStateReceivedEvents.TryGetValue(worldStateWrapper.ID, out SyncableStateReceiveEvent syncableStateReceiveEvent))
                    {
                        try
                        {
                            //Debug.Log("Emit event to " + worldStateWrapper.ID);
                            syncableStateReceiveEvent.Invoke(worldStateWrapper.StateBytes);
                        }
                        catch (System.Exception ex)
                        {
                            V_Logger.Error("Error receiving syncable data - " + ex.StackTrace + " - " + ex.Message);
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
                WorldStateBundle worldStateBundle = new(_outgoingSyncableStateBufferSnapshot);
                WorldStateSnapshot worldStateSnapshot = new(instanceCodeOfSnapshot, worldStateBundle);

                _commsHandler.SendWorldStateSnapshot(worldStateSnapshot.Bytes);
                _outgoingSyncableStateBufferSnapshot.Clear();
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