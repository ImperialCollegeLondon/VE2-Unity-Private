using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ViRSE.FrameworkRuntime;
using ViRSE.PluginRuntime.VComponents;

namespace ViRSE.PluginRuntime
{
    //TODO - are we calling things WorldState or SyncableState???

    public class WorldStateSyncer
    {
        public static WorldStateSyncer Instance;

        private IPluginWorldStateCommsHandler _commsHandler;
        private string _localInstanceCode;
        private int _cycleNumber = 0;

        [ReadOnly][SerializeField] private int _numberOfSyncablesRegisteredDebug = 0;
        //[SerializeField] private bool printNonHostTransmissionData = false;

        private Dictionary<string, WorldstateSyncableModule> syncablesAgainstIDs = new();
        private List<WorldStateBundle> _incommingWorldStateBundleBuffer = new();

        public bool IsHost => PluginSyncService.Instance.IsHost; //TODO

        public WorldStateSyncer(IPluginWorldStateCommsHandler commsHandler)
        {
            Instance = this;
            _commsHandler = commsHandler;

            //Need to remove this on destroy
            _commsHandler.OnReceiveWorldStateSyncableBundle += HandleReceiveWorldStateBundle;
        }

        public WorldStateSyncer() { } //For mocking

        //Happens if we move between instances of the same plugin
        //If changing plugins, this whole syncer will be destroyed and recreated
        public void ResetForNewInstance()
        {
            //May need to tell all the syncables to wipe their data here
        }

        public void RegisterWithSyncer(WorldstateSyncableModule worldStateSyncableModule)
        {
            Debug.Log("REg with syncer");
            syncablesAgainstIDs.Add(worldStateSyncableModule.ID, worldStateSyncableModule);
            _numberOfSyncablesRegisteredDebug++;
        }

        public void DerigsterFromSyncer(string id)
        {
            _numberOfSyncablesRegisteredDebug--;

            syncablesAgainstIDs.Remove(id);
        }

        private void HandleReceiveWorldStateBundle(byte[] byteData)
        {
            //Debug.Log("Rec state in syncer");
            WorldStateBundle worldStateBundle = new(byteData);
            _incommingWorldStateBundleBuffer.Add(worldStateBundle);
        }

        public void HandleNetworkUpdate()
        {
            IncrementCycle();
            CheckForDestroyedSyncables();
            CollectAndSendWorldStates();
            ProcessReceivedWorldStates();
        }

        private void CheckForDestroyedSyncables()
        {
            foreach (string id in syncablesAgainstIDs.Where(pair => pair.Value == null).Select(pair => pair.Key).ToList())
                syncablesAgainstIDs.Remove(id);
        }

        private void IncrementCycle()
        {
            _cycleNumber++;
            foreach (WorldstateSyncableModule syncable in syncablesAgainstIDs.Values)
                syncable.HandleCycleIncrement();
        }

        private void CollectAndSendWorldStates()
        {
            List<WorldStateWrapper> outgoingSyncableStateBufferTCP = new();
            List<WorldStateWrapper> outgoingSyncableStateBufferUDP = new();

            foreach (KeyValuePair<string, WorldstateSyncableModule> pair in syncablesAgainstIDs)
            {
                if (pair.Value.TryGetStateToTransmit(_cycleNumber, IsHost, out byte[] state, out TransmissionProtocol protocol))
                {
                    WorldStateWrapper worldStateWrapper = new(pair.Key, state);

                    if (protocol == TransmissionProtocol.TCP)
                        outgoingSyncableStateBufferTCP.Add(worldStateWrapper);
                    else
                        outgoingSyncableStateBufferUDP.Add(worldStateWrapper);
                }
            }

            if (outgoingSyncableStateBufferTCP.Count > 0)
            {
                WorldStateBundle TCPBundle = new(outgoingSyncableStateBufferTCP);
                _commsHandler.SendWorldStateBundle(TCPBundle.Bytes, TransmissionProtocol.TCP);
            }

            if (outgoingSyncableStateBufferUDP.Count > 0)
            {
                WorldStateBundle UDPBundle = new(outgoingSyncableStateBufferUDP);
                _commsHandler.SendWorldStateBundle(UDPBundle.Bytes, TransmissionProtocol.UDP);
            }
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
                    if (syncablesAgainstIDs.TryGetValue(worldStateWrapper.ID, out WorldstateSyncableModule syncable))
                    {
                        try
                        {
                            //Debug.Log("Emit event to " + worldStateWrapper.ID);
                            syncable.ReceiveStateFromNetwork(worldStateWrapper.StateBytes);
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
            List<WorldStateWrapper> outgoingSyncableStateBufferSnapshot = new();

            foreach (KeyValuePair<string, WorldstateSyncableModule> pair in syncablesAgainstIDs)
            {
                if (pair.Value.TryGetStateForSnapshot(out byte[] stateBytes))
                {
                    WorldStateWrapper worldStateWrapper = new(pair.Key, stateBytes);
                    outgoingSyncableStateBufferSnapshot.Add(worldStateWrapper);
                }
            }

            if (outgoingSyncableStateBufferSnapshot.Count > 0)
            {
                WorldStateBundle worldStateBundle = new(outgoingSyncableStateBufferSnapshot);
                WorldStateSnapshot worldStateSnapshot = new(instanceCodeOfSnapshot, worldStateBundle);

                _commsHandler.SendWorldStateSnapshot(worldStateSnapshot.Bytes);
            }

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