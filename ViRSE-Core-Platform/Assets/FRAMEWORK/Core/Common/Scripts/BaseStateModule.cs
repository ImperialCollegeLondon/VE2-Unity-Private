using System;
using System.Data.Common;
using System.IO;
using UnityEngine;
using ViRSE.Core;
using ViRSE.Core.Shared;
using static ViRSE.Core.Shared.CoreCommonSerializables;

//TODO - namespace?

[Serializable]
public class BaseStateConfig
{
    [BeginGroup(Style = GroupStyle.Round, ApplyCondition = true)]
    [Title("Transmission Settings", ApplyCondition = true)]
    [HideIf(nameof(MultiplayerSupportPresent), false)]
    [SerializeField] public bool IsNetworked = true;

    [HideIf(nameof(MultiplayerSupportPresent), false)]
    [DisableIf(nameof(IsNetworked), false)]
    [EndGroup(ApplyCondition = true)]
    [Space(5)]
    [SerializeField, IgnoreParent] public RepeatedTransmissionConfig RepeatedTransmissionConfig = new();

    [SerializeField, HideInInspector] public bool MultiplayerSupportPresent => MultiplayerSupport != null;
    public IMultiplayerSupport MultiplayerSupport => ViRSECoreServiceLocator.Instance.MultiplayerSupport;
}

public abstract class BaseStateModule : IStateModule
{
    public ViRSESerializable State { get; private set; }
    protected BaseStateConfig Config { get; private set; }
    public string ID { get; private set; }
    public string GameObjectName { get; private set; }  

    //public event Action OnBytesUpdated;

    public bool IsNetworked => Config.IsNetworked;
    public byte[] StateAsBytes { get => State.Bytes; set => UpdateBytes(value); }
    public TransmissionProtocol TransmissionProtocol => Config.RepeatedTransmissionConfig.TransmissionType;
    public float TransmissionFrequency => Config.RepeatedTransmissionConfig.TransmissionFrequency;


    public BaseStateModule(ViRSESerializable state, BaseStateConfig config, string goName, string syncType)
    {
        State = state;
        Config = config;
        GameObjectName = goName;
        ID = syncType + ":" + goName;

        ViRSECoreServiceLocator.Instance.RegisterStateModule(this);
    }

    public void TearDown()
    {
        if (ViRSECoreServiceLocator.Instance != null)
            ViRSECoreServiceLocator.Instance.DeregisterStateModule(this);
    }

    protected abstract void UpdateBytes(byte[] newBytes);
}
