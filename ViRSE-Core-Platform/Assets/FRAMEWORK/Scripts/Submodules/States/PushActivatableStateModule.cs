using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable] 
public class ActivatableStateConfig
{
    [SerializeField] public UnityEvent OnActivate;
    [SerializeField] public UnityEvent OnDeactivate;
}

public class PushActivatableStateModule
{
    private ActivatableStateConfig config;
    public PushActivatableState State { get; private set; } = new("test", 0, false, -1); //The network module has to see this 
    //Doesn't that mean it makes more sense living in the VC?

    public PushActivatableStateModule(ActivatableStateConfig config)
    {
        this.config = config; 
    }

    public void InvertState(InteractorID interactorID)
    {
        State.isActivated = !State.isActivated;

        if (State.isActivated)
            InvokeCustomerOnActivateEvent();
        else
            InvokeCustomerOnDeactivateEvent();

        State.activatingUserID = State.isActivated ? interactorID.ClientID : -1;
    }

    public void SetState(PushActivatableState newState)
    {
        bool oldIsActivated = State.isActivated;
        State = newState;

        if (!oldIsActivated && newState.isActivated)
            InvokeCustomerOnActivateEvent();
        else if (oldIsActivated && !newState.isActivated)
            InvokeCustomerOnDeactivateEvent();
    }

    private void InvokeCustomerOnActivateEvent()
    {
        config.OnActivate?.Invoke();
    }

    private void InvokeCustomerOnDeactivateEvent()
    {
        config.OnDeactivate.Invoke();
    }
}

public class PushActivatableState : PredictiveSyncableState
{
    public bool isActivated;
    public int activatingUserID;

    public PushActivatableState(string id, int stateChangeNumber, bool isActivated, int activatingUserID) : base(id, stateChangeNumber)
    {
        this.isActivated = isActivated;
        this.activatingUserID = activatingUserID;
    }

    public override bool CompareState(PredictiveSyncableState otherState)
    {
        PushActivatableState other = (PushActivatableState)otherState;

        return (isActivated == other.isActivated && activatingUserID == other.activatingUserID);
    }
}

//State object could have an OnChanged event, that way, the syncer would know to update?
//hhmn, maybe it should be the VC that handles the state change flag 
//It wants to raise on interact, but it doesn't want to raise if the state changed because the host said it should

//if the host tells us to force drop a grabbable, what happens? The interaction module has to know 
//Right, that's why the VC decides what happens when we receive remote state changes 
