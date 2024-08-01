using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PrimaryServerService : MonoBehaviour
{
    //public UnityEvent OnRegisterWithServer() 

    private IPrimaryServiceCommsService commsService;

    public void Initialize(ServerType serverType)
    {
        commsService = gameObject.AddComponent<DarkRiftCommsService>();
    }

    //Stuff other components need to know about 
    //When we connect 
    //When we disconnect
    //When we decide to route to a new instance 
    //Instance sync stuff, worldstate, playerstate,instant messages, etc
    //When the netcode version doesn't match


    //Stuff the customer needs to know about 
    //Host status changes
    //Admin status changes 
    //When people join or leave the instance 


    /* How should this actually work
     * PrimaryServerService contains a reference to the DarkRift layer, we'll call this CommsHandler?
     * Comms handler should emit events back up to the PrimaryServerService when it needs to, we wont need this for all the messages though
     * Basically, we can abstract away the actual details of the network here, we can just say "CommsHandler.Register with server"...
     * ...And the commshandler can then deal with the flow, IE 
     */


    /* 
     * Currently, we just send a big fat list of client infos, 
     * We could send actual messages for "ClientJoin and ClientLeave"
     * Actually, let's keep it as it is, means we can drop connection and come back and continue as normal
     */
}

public struct ServerRegistration
{
    public int ClientID;
    public bool CompletedTutorial;
    public UserSettings UserSettings; 
}

public struct UserSettings
{
    public string displayName; 

    //Avatar settings 
    //Control settings 
    //All the stuff stored on the server
}
