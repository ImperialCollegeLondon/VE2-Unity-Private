using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using static VE2.Common.CommonSerializables;

public abstract class BaseWorldStateModule : BaseStateModule, IWorldStateModule
{
    public string ID { get; private set; }
    public byte[] StateAsBytes { get => State.Bytes; set => UpdateBytes(value); }
    protected abstract void UpdateBytes(byte[] newBytes);

    public BaseWorldStateModule(VE2Serializable state, BaseStateConfig config, string id, BaseStateModuleContainer stateModuleContainer) : base(state, config, stateModuleContainer)
    {
        ID = id;
    }
}