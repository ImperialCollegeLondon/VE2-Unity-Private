using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//Has ID, frequency, sync offset, decides when to transmit to the syncer 
//TODO, The ID stuff can be split into a sub component, away from the frequency and sync offset stuff 
[Serializable]
public class WorldstateSyncableModule
{
    [VerticalGroup("NetworkSettings_VGroup")]
    [FoldoutGroup("NetworkSettings_VGroup/Network Settings")]
    //[PropertyOrder(1000)]
    [InfoBox("Careful with high sync frequencies, the network load can impact performance!", InfoMessageType.Warning, "@this.syncFrequency > 5")]
    [SuffixLabel("Hz")]
    [Range(0.2f, 50f)]
    [SerializeField] private float syncFrequency;
    public float SyncFrequency => syncFrequency;

    [SerializeField, ShowInInspector, HideLabel]
    [FoldoutGroup("NetworkSettings_VGroup/Network Settings")]
    private ProtocolModule protocolModule;

    [SerializeField, HideInInspector] private GameObject gameObject;

    [SerializeField, HideInInspector] private string syncType;

    [SerializeField, HideInInspector] private string id;
    public string ID => id;

    private static Dictionary<string, WorldstateSyncableModule> worldStateSyncableModulesAgainstIDs = new();
    private bool CheckForRegistrationError() =>
    worldStateSyncableModulesAgainstIDs.TryGetValue(id, out WorldstateSyncableModule module) && module != this;

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

    public WorldstateSyncableModule(GameObject gameObject, string syncType)
    {
        this.gameObject = gameObject;
        this.syncType = syncType;

        syncFrequency = 1f;

        protocolModule = new();

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
        if (syncFrequency > 1)
            syncFrequency = Mathf.RoundToInt(syncFrequency);
    }

    private void RefreshID()
    {
        //Remove old ID from dict
        if (ID != null && worldStateSyncableModulesAgainstIDs.TryGetValue(ID, out WorldstateSyncableModule module) && module != null && module == this)
                worldStateSyncableModulesAgainstIDs.Remove(ID);

        //Create new ID
        id = syncType + "-" + gameObject.name;

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
                WorldStateSyncService.AddStateToOutgoingBuffer(syncableState, protocolModule.transmissionType);
        }

        newStateToTransmit = false;
    }

    private void SetupHostSyncOffset()
    {
        //We'd need to subtract 1 if we've already added one
        if (syncOffsetSetup)
            numOfSyncablesPerSyncOffsets[hostSyncOffset]--;

        hostSyncInterval = ((int)(50 / syncFrequency)); //a frequency of 1 should send messages every 50 fixedupdate frames 

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

        if (syncFrequency > 1)
            newFrequency = Mathf.RoundToInt(newFrequency);

        syncFrequency = newFrequency;
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
