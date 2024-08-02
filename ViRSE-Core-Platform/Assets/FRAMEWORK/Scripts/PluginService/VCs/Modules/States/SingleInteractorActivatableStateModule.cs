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

public class SingleInteractorActivatableStateModule : ISingleInteractorActivatableStateModule
{
    #region plugin interfaces
    UnityEvent ISingleInteractorActivatableStateModule.OnActivate => _config.OnActivate; 
    UnityEvent ISingleInteractorActivatableStateModule.OnDeactivate => _config.OnDeactivate; 
    bool ISingleInteractorActivatableStateModule.IsActivated { get => State.IsActivated; set => ReceiveNewActivationStateFromCustomer(value);}
    InteractorID ISingleInteractorActivatableStateModule.CurrentInteractor => State.CurrentInteractor;
    #endregion

    private ActivatableStateConfig _config;
    public SingleInteractorActivatableState State { get; private set; } = new("test", 0, false, null); //The network module has to see this 

    public UnityEvent OnPluginChangedState { get; private set; } = new();

    public SingleInteractorActivatableStateModule(ActivatableStateConfig config)
    {
        _config = config;
    }

    private void ReceiveNewActivationStateFromCustomer(bool newIsActivated)
    {
        SingleInteractorActivatableState newState = new(State.id, State.stateChangeNumber, newIsActivated, null);
        if (newState.IsActivated != State.IsActivated)
            OnPluginChangedState?.Invoke(); //Inform the VC the plugin has induced a state change, so that the VC can raise the state change flag 

        SetState(newState);
    }

    public void InvertState(InteractorID interactorID)
    {
        State.IsActivated = !State.IsActivated;

        if (State.IsActivated)
            InvokeCustomerOnActivateEvent();
        else
            InvokeCustomerOnDeactivateEvent();

        State.CurrentInteractor = State.IsActivated ? interactorID : null;
    }

    public void SetState(SingleInteractorActivatableState newState)
    {
        bool oldIsActivated = State.IsActivated;
        State = newState;

        if (!oldIsActivated && newState.IsActivated)
            InvokeCustomerOnActivateEvent();
        else if (oldIsActivated && !newState.IsActivated)
            InvokeCustomerOnDeactivateEvent();
    }

    private void InvokeCustomerOnActivateEvent()
    {
        _config.OnActivate?.Invoke();
    }

    private void InvokeCustomerOnDeactivateEvent()
    {
        _config.OnDeactivate.Invoke();
    }
}

public class SingleInteractorActivatableState : PredictiveSyncableState
{
    public bool IsActivated { get; set; }
    public InteractorID CurrentInteractor { get; set; }

    public SingleInteractorActivatableState(string id, int stateChangeNumber, bool isActivated, InteractorID CurrentInteractor) : base(id, stateChangeNumber)
    {
        this.IsActivated = isActivated;
        this.CurrentInteractor = CurrentInteractor;
    }

    public override bool CompareState(PredictiveSyncableState otherState)
    {
        SingleInteractorActivatableState other = (SingleInteractorActivatableState)otherState;

        return (IsActivated == other.IsActivated && CurrentInteractor == other.CurrentInteractor);
    }
}