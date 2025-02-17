using UnityEngine;
using VE2.Common;

internal interface IInstanceProvider
{
    public IInstanceService InstanceService { get; }
    public string GameObjectName { get; }
    public bool IsEnabled { get; }
}
