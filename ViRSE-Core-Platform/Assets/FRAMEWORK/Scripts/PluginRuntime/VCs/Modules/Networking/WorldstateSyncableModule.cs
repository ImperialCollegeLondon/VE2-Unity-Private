using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using ViRSE.FrameworkRuntime;
using ViRSE.PluginRuntime.VComponents;

namespace ViRSE.PluginRuntime.VComponents
{
    [Serializable]
    public class WorldStateSyncableConfig //: ISerializationCallbackReceiver
    {
        [VerticalGroup("NetworkSettings_VGroup")]
        [FoldoutGroup("NetworkSettings_VGroup/Network Settings")]
        [InfoBox("Careful with high sync frequencies, the network load can impact performance!", InfoMessageType.Warning, "@this.SyncFrequency > 5")]
        [SuffixLabel("Hz")]
        [Range(0.2f, 50f)]
        [SerializeField] public float SyncFrequency = 1;

        [FoldoutGroup("NetworkSettings_VGroup/Network Settings")]
        [SerializeField, HideLabel] public ProtocolConfig protocolConfig;

        [SerializeField, HideInInspector] private string syncType; //TODO, figure out what's going on with this...
        public string SyncType => syncType;

        public WorldStateSyncableConfig(string syncType)
        {
            this.syncType = syncType;
        }

        protected virtual void OnValidate() //TODO - OnVlidate needs to come from VC
        {
            if (SyncFrequency > 1)
                SyncFrequency = Mathf.RoundToInt(SyncFrequency);
        }

        //    // Called when the object is deserialized
        //    public void OnAfterDeserialize()
        //    {
        //        // Register the instance with the manager
        //    }

        //    // Called before the object is serialized
        //    public void OnBeforeSerialize()
        //    {
        //        // Optional: Code to execute before serialization
        //    }

        //    private static Dictionary<string, WorldstateSyncableModule> worldStateSyncableModulesAgainstIDs = new();
        //    private bool CheckForRegistrationError() =>
        //    worldStateSyncableModulesAgainstIDs.TryGetValue(ID, out WorldstateSyncableModule module) && module != this;

        //    [PropertyOrder(-100000)]
        //    //[InfoBox("Error - syncables must have unique GameObject names! Please rename this GameObject", InfoMessageType.Error, "@CheckIfNameClash()")]
        //    [Button]
        //    [PropertySpace(SpaceBefore = 10, SpaceAfter = 10)]
        //    [ShowIf("@CheckForRegistrationError()")]
        //    public void Rename(bool renamedManually = true)
        //    {
        //        string newID;
        //        int extraIDNumber = 0;

        //        do
        //        {
        //            extraIDNumber++;
        //            newID = syncType + "-" + gameObject.name + extraIDNumber.ToString();
        //        }
        //        while (worldStateSyncableModulesAgainstIDs.ContainsKey(newID));

        //        gameObject.name += extraIDNumber;
        //        RefreshID();

        //#if UNITY_EDITOR
        //        if (renamedManually)
        //            Undo.RecordObject(gameObject, "Rename Object"); // Record the object for undo
        //#endif

        //    }

        //private void RefreshID()
        //{
        //    //Remove old ID from dict
        //    if (ID != null && worldStateSyncableModulesAgainstIDs.TryGetValue(ID, out WorldstateSyncableModule module) && module != null && module == this)
        //        worldStateSyncableModulesAgainstIDs.Remove(ID);

        //    //Create new ID
        //    ID = syncType + "-" + gameObject.name;

        //    //Add new ID to dict if not already present
        //    if (worldStateSyncableModulesAgainstIDs.ContainsKey(ID))
        //        worldStateSyncableModulesAgainstIDs.Add(ID, this);
        //}

    }

    //Has ID, frequency, sync offset, decides when to transmit to the syncer 
    //TODO, The ID stuff can be split into a sub component, away from the frequency and sync offset stuff 
    public class WorldstateSyncableModule : IWorldStateSyncableModule
    {
        #region Plugin Interfaces
        float IWorldStateSyncableModule.SyncFrequency {
            get => _config.SyncFrequency;
            set {
                _config.SyncFrequency = value;
                if (_config.SyncFrequency > 1)
                    _config.SyncFrequency = Mathf.RoundToInt(_config.SyncFrequency);
            }
        }
        TransmissionProtocol IWorldStateSyncableModule.TransmissionProtocol { get => _protocolModule.TransmissionProtocol; set => _protocolModule.TransmissionProtocol = value; }
        #endregion

        private WorldStateSyncableConfig _config;
        protected ViRSENetworkSerializable _state;
        protected string _goName { get; private set; }

        private ProtocolModule _protocolModule;

        public string ID { get; private set; }

        //We don't want host to blast out state for everything every x frames, should instead stagger them to even network load
        private int _hostSyncOffset;
        private int _hostSyncInterval; //How many frames go by before syncing

        private bool _syncOffsetSetup = false;
        private static Dictionary<int, int> _numOfSyncablesPerSyncOffsets = new();

        private bool _forceTransmit;

        public UnityEvent<byte[]> OnStateReceive { get; private set; } = new();

        public WorldstateSyncableModule(WorldStateSyncableConfig config, ViRSENetworkSerializable state, WorldStateSyncer worldStateSyncer, string goName)
        {
            _config = config;
            _state = state;
            _goName = goName;

            _protocolModule = new(_config.protocolConfig); //TODO - Inject!

            SetupHostSyncOffset();

            ID = _config.SyncType + ":" + _goName;

            worldStateSyncer.RegisterWithSyncer(this);
        }

        public virtual void ReceiveStateFromNetwork(byte[] stateAsBytes)
        {
            OnStateReceive?.Invoke(stateAsBytes);
        }

        public bool TryGetStateForSnapshot(out byte[] stateAsBytes)
        {
            if (_state != null)
            {
                stateAsBytes = _state.Bytes;
                return true;
            }
            else
            {
                stateAsBytes = null;
                return false;
            }
        }

        public virtual void HandleCycleIncrement() { } //Nothing to do here, but can be overriden by child classes

        public bool TryGetStateToTransmit(int cycleNumber, bool isHost, out byte[] state, out TransmissionProtocol protocol)
        {
            state = null;
            protocol = _config.protocolConfig.TransmissionType;

            if (_state == null)
                return false;

            bool onBroadcastFrame = isHost && (cycleNumber + _hostSyncOffset) % _hostSyncInterval == 0;
            bool shouldForceTransmitThisFrame = _forceTransmit;
            _forceTransmit = false;

            if (onBroadcastFrame || shouldForceTransmitThisFrame)
            {
                state = _state.Bytes;
                return true;
            } 
            else
            {
                return false;
            }
        }

        public void ForceTransmitNextCycle()
        {
            _forceTransmit = true;
        }

        private void SetupHostSyncOffset()
        {
            //We'd need to subtract 1 if we've already added one
            if (_syncOffsetSetup)
                _numOfSyncablesPerSyncOffsets[_hostSyncOffset]--;

            _hostSyncInterval = ((int)(50 / _config.SyncFrequency)); //a frequency of 1 should send messages every 50 fixedupdate frames 

            //To smooth out the transmission load, choose the least used offset
            int leastUsedSyncOffset = 0;
            int timesLeastUsedSyncOffsetUsed = 10000;

            for (int i = 0; i < 50; i++)
            {
                int timesSyncOffsetUsed = _numOfSyncablesPerSyncOffsets.ContainsKey(i) ?
                    _numOfSyncablesPerSyncOffsets[i] : 0;

                if (timesSyncOffsetUsed < timesLeastUsedSyncOffsetUsed)
                {
                    timesLeastUsedSyncOffsetUsed = timesSyncOffsetUsed;
                    leastUsedSyncOffset = i;
                }

                if (timesLeastUsedSyncOffsetUsed == 0)
                    break; //No need to keep searching, can't be less than zero!
            }

            _hostSyncOffset = leastUsedSyncOffset;

            if (_numOfSyncablesPerSyncOffsets.ContainsKey(_hostSyncOffset))
                _numOfSyncablesPerSyncOffsets[_hostSyncOffset]++;
            else
                _numOfSyncablesPerSyncOffsets.Add(_hostSyncOffset, 1);

            _syncOffsetSetup = true;
        }

        public void SetSyncFrequency(float newFrequency)
        {
            if (newFrequency < 0)
            {
                V_Logger.Error("Tried to set sync frequency to below zero on the " + GetType() + " on " + _goName + ", this is not allowed!");
                return;
            }

            if (_config.SyncFrequency > 1)
                newFrequency = Mathf.RoundToInt(newFrequency);

            _config.SyncFrequency = newFrequency;
            SetupHostSyncOffset();
        }
    }
}
