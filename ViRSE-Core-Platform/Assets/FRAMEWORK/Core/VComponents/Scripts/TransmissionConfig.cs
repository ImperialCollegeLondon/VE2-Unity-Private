using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViRSE;
using ViRSE.Core.Shared;

[Serializable]
public class RepeatedTransmissionConfig : TransmissionConfig
{
    [BeginGroup("Transmission Settings", Style = GroupStyle.Boxed)]
    [Suffix("Hz")]
    [Range(0.2f, 50f)]
    [SerializeField] public float TransmissionFrequency = 1;

    protected virtual void OnValidate() //TODO - OnVlidate needs to come from VC
    {
        if (TransmissionFrequency > 1)
            TransmissionFrequency = Mathf.RoundToInt(TransmissionFrequency);
    }
}

[Serializable]
public class TransmissionConfig
{
    [SerializeField] public TransmissionProtocol TransmissionType;

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
