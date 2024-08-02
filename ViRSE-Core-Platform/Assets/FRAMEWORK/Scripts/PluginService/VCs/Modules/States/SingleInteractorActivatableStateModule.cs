using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ViRSE.VComponents
{
    [Serializable]
    public class ActivatableStateConfig
    {
        [SerializeField] public UnityEvent OnActivate;
        [SerializeField] public UnityEvent OnDeactivate;
    }

    public class SingleInteractorActivatableStateModule : MonoBehaviour, ISingleInteractorActivatableStateModule
    {
        #region plugin interfaces
        UnityEvent ISingleInteractorActivatableStateModule.OnActivate => _config.OnActivate;
        UnityEvent ISingleInteractorActivatableStateModule.OnDeactivate => _config.OnDeactivate;
        bool ISingleInteractorActivatableStateModule.IsActivated { get => State.IsActivated; set => ReceiveNewActivationStateFromCustomer(value); }
        InteractorID ISingleInteractorActivatableStateModule.CurrentInteractor => State.CurrentInteractor;
        #endregion

        private ActivatableStateConfig _config;
        public SingleInteractorActivatableState State { get; private set; }
        public UnityEvent OnPluginChangedState { get; private set; } = new();

        public void Initialize(ActivatableStateConfig config)
        {
            _config = config;
            State = new(gameObject.name, 0, false, null); //TODO - remove ID from state
        }

        private void ReceiveNewActivationStateFromCustomer(bool newIsActivated)
        {
            if (newIsActivated != State.IsActivated)
                InvertState(null);
        }

        public void InvertState(InteractorID interactorID)
        {
            State.IsActivated = !State.IsActivated;
            State.CurrentInteractor = State.IsActivated ? interactorID : null;

            if (State.IsActivated)
                InvokeCustomerOnActivateEvent();
            else
                InvokeCustomerOnDeactivateEvent();
        }

        public void UpdateToReceivedNetworkState(SingleInteractorActivatableState newState)
        {
            if (newState.IsActivated == State.IsActivated)
                return;

            State = newState;

            if (State.IsActivated)
                InvokeCustomerOnActivateEvent();
            else
                InvokeCustomerOnDeactivateEvent();
        }

        private void InvokeCustomerOnActivateEvent()
        {
            try
            {
                _config.OnActivate?.Invoke();
            }
            catch (Exception e)
            {
                Debug.Log($"Error when emitting OnActivate from {gameObject.name} \n{e.Message}\n{e.StackTrace}");
            }
        }

        private void InvokeCustomerOnDeactivateEvent()
        {
            try
            {
                _config.OnDeactivate?.Invoke();
            }
            catch (Exception e)
            {
                Debug.Log($"Error when emitting OnDeactivate from {gameObject.name} \n{e.Message}\n{e.StackTrace}");
            }
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
}