using UnityEngine;

internal interface IWorldStateSyncProvider //TODO: Internal! Same as the player one, same as the actual SyncService one 
{
    public IWorldStateSyncService WorldStateSyncService { get; }
    public string GameObjectName { get; }
    public bool IsEnabled { get; }
}
