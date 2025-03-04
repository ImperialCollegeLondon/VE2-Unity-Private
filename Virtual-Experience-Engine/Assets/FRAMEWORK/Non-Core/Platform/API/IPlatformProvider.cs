using UnityEngine;

internal interface IPlatformProvider
{
    public IPlatformService PlatformService { get; }
    public string GameObjectName { get; }
}
