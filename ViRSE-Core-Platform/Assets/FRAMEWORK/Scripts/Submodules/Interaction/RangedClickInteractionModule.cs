
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class RangedClickInteractionModule : RangedInteractionModule, IRangedClickInteractionModule
{
    public RangedClickInteractionModule(RangedInteractionConfig rangedInteractionConfig, GameObject gameObject) : base(rangedInteractionConfig, gameObject) 
    {
        Debug.Log("New UnityEvent!");
    }

    public UnityEvent<InteractorID> OnClickDown { get; private set; } = new UnityEvent<InteractorID>(); //HELP! How is this null?

    public void OnComponentAwake()
    {
        OnClickDown = new();
    }


    public void InvokeOnClickDown(InteractorID interactorID)
    {
        //only happens if is valid click
        OnClickDown.Invoke(interactorID);

        /*Do we actually need the interactor ID?
         * Well, what would we use it for?
         * Telling the customer who activated the thing?
         * NOT for syncing, that's done via a separate system
         * 
         */
    }
}

//We'll be re-using this component for ranged hold buttons too
//NO, WE WON'T! Hold buttons aren't interacted with directly, it's the button itself that looks at the interactors  
//So... what, we just have OnClickDown here?? 
//If it's hold, the interactor wants to vibrate

//The interactor needs to know if this is a hold type or not??

//Unless we DO use OnClickDown and OnClickUp for hold, and the activatable wires it into some non-WSS networking component?
//It just does seem to make more sense to have that data live in the interactor itself 

//In which case, the interactor DOES need to be able to see range... but doesn't want to be firing off "OnClick" 

//THE APPROACH...............
//What if hold buttons still sync themselves, but just not via world state syncables 
//So we effectively need every non-host's button to be broadcasting out whether or not that client is holding it
//Host then aggregates this and broadcasts out everything else 
//SO the non-hosts still have this list of everyone, but they can only actually modify themselves 
//So, non-host detects click, they change their own internal list for "local holding interactors", and then transmit this to the host 
//RIGHT, so, when the non-host transmits to host, it's only local interactors, e.g "Client12: 0", or "Client1: 2" 
//Whenever the host transmits back to the non-host, its a list of everyone 
//This is different to the regular world state syncables, this time, the non-host still needs to broadcast 
//Ok, so we need another module?
//OR, we just pass a boolean to the worldstatesync module to say "broadcast from non-host"


public class InteractorID
{
    public int ClientID;
    public InteractorType interactorType; //TODO, do we really want this, we don't NEED This for toggle 

    public InteractorID(int clientID, InteractorType interactorType)
    {
        ClientID = clientID;
        this.interactorType = interactorType;
    }
}

public enum InteractorType
{
    TwoD,
    VRRight,
    VRLeft,
    Feet
}
