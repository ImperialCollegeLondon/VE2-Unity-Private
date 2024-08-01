using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Serializable]
public class WorldStateSyncableConfig
{
    [VerticalGroup("NetworkSettings_VGroup")]
    [FoldoutGroup("NetworkSettings_VGroup/Network Settings")]
    //[PropertyOrder(1000)]
    [InfoBox("Careful with high sync frequencies, the network load can impact performance!", InfoMessageType.Warning, "@this.syncFrequency > 5")]
    [SuffixLabel("Hz")]
    [Range(0.2f, 50f)]
    [SerializeField] public float syncFrequency = 1;

    [ShowInInspector, HideLabel]
    [FoldoutGroup("NetworkSettings_VGroup/Network Settings")]
    [SerializeField] public ProtocolConfig protocolConfig;

    [SerializeField] public string syncType = "";

    public WorldStateSyncableConfig(string syncType)
    {
        this.syncType = syncType;
    }
}

//Has ID, frequency, sync offset, decides when to transmit to the syncer 
//TODO, The ID stuff can be split into a sub component, away from the frequency and sync offset stuff 
[Serializable]
public class WorldstateSyncableModule
{
    private WorldStateSyncableConfig config;
    private BaseSyncableState state;
    private GameObject gameObject;

    private string syncType;

    public string ID { get; private set; }

    private static Dictionary<string, WorldstateSyncableModule> worldStateSyncableModulesAgainstIDs = new();
    private bool CheckForRegistrationError() =>
    worldStateSyncableModulesAgainstIDs.TryGetValue(ID, out WorldstateSyncableModule module) && module != this;

//    [PropertyOrder(-100000)]
//    //[InfoBox("Error - syncables must have unique GameObject names! Please rename this GameObject", InfoMessageType.Error, "@CheckIfNameClash()")]
//    [Button]
//    [PropertySpace(SpaceBefore = 10, SpaceAfter = 10)]
//    //[ShowIf("@CheckForRegistrationError()")]
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

    public WorldstateSyncableModule(WorldStateSyncableConfig worldStateSyncableConfig, BaseSyncableState state, GameObject gameObject, string syncType)
    {
        this.state = state;
        this.config = worldStateSyncableConfig;
        this.gameObject = gameObject;
        this.syncType = syncType;

        RefreshID();
        Debug.Log("New syncable, ID is " + ID);
    }

    public void UpdateSyncableData(BaseSyncableState syncableState, bool shouldTransmit)
    {
        this.syncableState = syncableState;
        newStateToTransmit = shouldTransmit;
    }

    protected virtual void OnValidate() //TODO - OnVlidate needs to come from VC
    {
        if (config.syncFrequency > 1)
            config.syncFrequency = Mathf.RoundToInt(config.syncFrequency);
    }

    private void RefreshID()
    {
        //Remove old ID from dict
        if (ID != null && worldStateSyncableModulesAgainstIDs.TryGetValue(ID, out WorldstateSyncableModule module) && module != null && module == this)
                worldStateSyncableModulesAgainstIDs.Remove(ID);

        //Create new ID
        ID = syncType + "-" + gameObject.name;

        //Add new ID to dict if not already present
        if (worldStateSyncableModulesAgainstIDs.ContainsKey(ID))
            worldStateSyncableModulesAgainstIDs.Add(ID, this);
    }

    //We don't want host to blast out state for everything every x frames, should instead stagger them to even network load
    private int hostSyncOffset;
    private int hostSyncInterval; //How many frames go by before syncing

    private bool syncOffsetSetup = false;
    public static Dictionary<int, int> numOfSyncablesPerSyncOffsets = new();

    protected bool newStateToTransmit { get; private set; }
    public BaseSyncableState syncableState { get; private set; } = null;

    protected virtual void Start() //TODO tie into VC
    {
        SetupHostSyncOffset();
    }

    protected virtual void FixedUpdate() //TODO tie into VC
    {
        bool onBroadcastFrame = InstanceSyncService.IsHost && (StaticData.fixedUpdateFrame + hostSyncOffset) % hostSyncInterval == 0;

        if (ID != null && (onBroadcastFrame || newStateToTransmit))
        {
            //TODO, below comment, no longer just plugin syncables! Now its all syncables that start will null
            if (syncableState != null) //PluginSyncables that haven't yet received state will be null
                WorldStateSyncService.AddStateToOutgoingBuffer(syncableState, config.protocolConfig.transmissionType);
        }

        newStateToTransmit = false;
    }

    private void SetupHostSyncOffset()
    {
        //We'd need to subtract 1 if we've already added one
        if (syncOffsetSetup)
            numOfSyncablesPerSyncOffsets[hostSyncOffset]--;

        hostSyncInterval = ((int)(50 / config.syncFrequency)); //a frequency of 1 should send messages every 50 fixedupdate frames 

        //To smooth out the transmission load, choose the least used offset
        int leastUsedSyncOffset = 0;
        int timesLeastUsedSyncOffsetUsed = 10000;

        for (int i = 0; i < 50; i++)
        {
            int timesSyncOffsetUsed = numOfSyncablesPerSyncOffsets.ContainsKey(i) ?
                numOfSyncablesPerSyncOffsets[i] : 0;

            if (timesSyncOffsetUsed < timesLeastUsedSyncOffsetUsed)
            {
                timesLeastUsedSyncOffsetUsed = timesSyncOffsetUsed;
                leastUsedSyncOffset = i;
            }

            if (timesLeastUsedSyncOffsetUsed == 0)
                break; //No need to keep searching, can't be less than zero!
        }

        hostSyncOffset = leastUsedSyncOffset;

        if (numOfSyncablesPerSyncOffsets.ContainsKey(hostSyncOffset))
            numOfSyncablesPerSyncOffsets[hostSyncOffset]++;
        else
            numOfSyncablesPerSyncOffsets.Add(hostSyncOffset, 1);

        syncOffsetSetup = true;
    }

    public SyncableStateReceiveEvent RegisterWithSyncerAndGetSyncableStateReceiveEvent() =>
        WorldStateSyncService.RegisterForSyncDataReceivedEvents(ID);

    public void SetSyncFrequency(float newFrequency)
    {
        if (newFrequency < 0)
        {
            V_Logger.Error("Tried to set sync frequency to below zero on the " + GetType() + " on " + gameObject.name + ", this is not allowed!");
            return;
        }

        if (config.syncFrequency > 1)
            newFrequency = Mathf.RoundToInt(newFrequency);

        config.syncFrequency = newFrequency;
        SetupHostSyncOffset();
    }

    protected virtual void OnDestroy()
    {
        if (syncOffsetSetup)
            numOfSyncablesPerSyncOffsets[hostSyncOffset]--;

        syncOffsetSetup = false;

        //Wont do anything if not registered already
        WorldStateSyncService.DeregisterListener(ID);
    }

}
