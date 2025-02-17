using UnityEngine;
using VE2.Common;

public interface IWorldStateSyncService
{
    public void RegisterWorldStateModule(IWorldStateModule module);
    public void DeregisterWorldStateModule(IWorldStateModule module);
    // public string GameObjectName { get; }
}


/*
    We don't need this interface if we use the container 
    Maybe we just stick with containers, and just have one 
    
    let's actually just remove the state module containers altogether 

    PLayer - needs access to OnConnectedToServer... actually, why? 
    Because we need the player ID before we can interact with anything 

    that's fine, so PlayerService has OnConnected.. but we want to be able to point IWorldStateSyncService directly to the syncer?
    so then the WorldStateSyncer needs to expose the LocalID

*/