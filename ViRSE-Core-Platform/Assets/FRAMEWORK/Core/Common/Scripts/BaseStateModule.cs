using System;
using System.IO;
using UnityEngine;
using ViRSE.Core.Shared;
using static ViRSE.Core.Shared.CoreCommonSerializables;

[Serializable]
public class 
BaseStateConfig
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
    public string GOName { get; private set; }

    protected BaseStateConfig Config { get; private set; }

    public event Action OnBytesUpdated;

    //TODO - when the state is written to, we need to trigger some event to handle it... i.e, make the light turn on!
    byte[] IStateModule.StateAsBytes { get => State.Bytes; set => UpdateBytes(value); }
    string IStateModule.GOName => GOName;
    TransmissionProtocol IStateModule.TransmissionProtocol => Config.RepeatedTransmissionConfig.TransmissionType;
    float IStateModule.TransmissionFrequency => Config.RepeatedTransmissionConfig.TransmissionFrequency;

    public BaseStateModule(ViRSESerializable state, BaseStateConfig config, string goName)
    {
        GOName = goName;
        State = state;

        Config = config;

        if (Config.MultiplayerSupportPresent && Config.IsNetworked)
        {
            Config.MultiplayerSupport.RegisterStateModule(this, GetType().Name, goName);
            //Debug.Log("VC registered with syncer");
        }
        else
        {
            //if (!Config.NetworkManagerPresent)
            //{
            //    Debug.Log("VC has not registered with syncer, no network manager");
            //}
        }
    }

    protected abstract void UpdateBytes(byte[] newBytes);
}
