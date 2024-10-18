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

public abstract class BaseStateModule : IBaseStateModule
{
    public ViRSESerializable State { get; private set; }
    protected BaseStateConfig Config { get; private set; }
    public string GameObjectName { get; private set; }
    private readonly BaseStateModuleContainer _baseStateContainer;

    //public event Action OnBytesUpdated;

    private bool _wasNetworkedLastFrame;
    public bool IsNetworked => Config.IsNetworked;
    public event Action<bool> OnIsNetworkedChanged;

    public TransmissionProtocol TransmissionProtocol => Config.RepeatedTransmissionConfig.TransmissionType;
    public float TransmissionFrequency => Config.RepeatedTransmissionConfig.TransmissionFrequency;


    public BaseStateModule(ViRSESerializable state, BaseStateConfig config, string goName, BaseStateModuleContainer baseStateContainer)
    {
        State = state;
        Config = config;
        GameObjectName = goName;

        _baseStateContainer = baseStateContainer;
        _baseStateContainer.RegisterStateModule(this);

        _wasNetworkedLastFrame = IsNetworked;
    }

    public void HandleFixedUpdate() 
    {
        if (IsNetworked && !_wasNetworkedLastFrame)
            OnIsNetworkedChanged?.Invoke(true);
        else if (!IsNetworked && _wasNetworkedLastFrame)
            OnIsNetworkedChanged?.Invoke(false);
    }

    public virtual void TearDown() => _baseStateContainer.DeregisterStateModule(this);

}

public abstract class BaseWorldStateModule : BaseStateModule, IWorldStateModule
{
    public string ID { get; private set; }
    public byte[] StateAsBytes { get => State.Bytes; set => UpdateBytes(value); }
    protected abstract void UpdateBytes(byte[] newBytes);

    public BaseWorldStateModule(ViRSESerializable state, BaseStateConfig config, string goName, string syncType, WorldStateModulesContainer worldStateModulesContainer) : base(state, config, goName, worldStateModulesContainer)
    {
        ID = syncType + ":" + goName;
    }
}
