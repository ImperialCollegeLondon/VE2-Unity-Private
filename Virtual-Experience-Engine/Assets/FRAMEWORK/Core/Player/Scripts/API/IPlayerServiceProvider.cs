using UnityEngine;

internal interface IPlayerServiceProvider
{
    public IPlayerService PlayerService { get; }
    public string GameObjectName { get; }
}
