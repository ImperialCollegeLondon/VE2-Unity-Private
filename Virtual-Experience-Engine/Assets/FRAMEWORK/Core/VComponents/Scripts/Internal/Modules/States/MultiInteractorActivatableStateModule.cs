using System;
using UnityEngine;
using UnityEngine.Events;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class HoldActivatableStateConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Activation Settings", ApplyCondition = true)]
        [SerializeField] public UnityEvent OnActivate = new();

        [EndGroup(Order = 1)]
        [SpaceArea(spaceAfter: 10, Order = -1), SerializeField] public UnityEvent OnDeactivate = new();

        [BeginGroup(Style = GroupStyle.Round, ApplyCondition = true)]
        [Title("Transmission Settings", ApplyCondition = true)]
        [EndGroup(ApplyCondition = true, Order = 5)]
        [SerializeField] public bool IsNetworked = true;

    }

    internal class MultiInteractorActivatableStateModule : IMultiInteractorActivatableStateModule
    {
        public UnityEvent OnActivate => _config.OnActivate;
        public UnityEvent OnDeactivate => _config.OnDeactivate;
        public bool IsActivated { get => _state.IsActivated; set => _state.IsActivated = value; }
        public ushort MostRecentInteractingClientID => _state.MostRecentInteractingClientID;

        private MultiInteractorActivatableState _state = null;
        private HoldActivatableStateConfig _config = null;

        public MultiInteractorActivatableStateModule(MultiInteractorActivatableState state, HoldActivatableStateConfig config, string id)
        {
            _state = state;
            _config = config;
        }

        // private void HandleExternalActivation(bool newIsActivated)
        // {
        //     if (newIsActivated != _state.IsActivated)
        //         InvertState(ushort.MaxValue);
        // }

        // public void InvertState(ushort clientID)
        // {
        //     _state.IsActivated = !_state.IsActivated;

        //     if (clientID != ushort.MaxValue)
        //         _state.MostRecentInteractingClientID = clientID;

        //     _state.StateChangeNumber++;

        //     if (_state.IsActivated)
        //         InvokeCustomerOnActivateEvent();
        //     else
        //         InvokeCustomerOnDeactivateEvent();
        // }

        public void SetState(ushort clientID, bool activationState)
        {
            _state.IsActivated = activationState;

            if (clientID != ushort.MaxValue)
                _state.MostRecentInteractingClientID = clientID;
            
            _state.StateChangeNumber++;

            if (_state.IsActivated)
                InvokeCustomerOnActivateEvent();
            else
                InvokeCustomerOnDeactivateEvent();

            Debug.Log($"StateModule: SetState = {_state.IsActivated}");
        }

        private void InvokeCustomerOnActivateEvent()
        {
            try
            {
                _config.OnActivate?.Invoke();
            }
            catch (Exception e)
            {
                Debug.Log($"Error when emitting OnActivate from activatable with ID {_state.MostRecentInteractingClientID} \n{e.Message}\n{e.StackTrace}");
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
                Debug.Log($"Error when emitting OnDeactivate from activatable with ID {_state.MostRecentInteractingClientID} \n{e.Message}\n{e.StackTrace}");
            }
        }

        public void TearDown()
        {

        }

        public void HandleFixedUpdate()
        {

        }
    }

    [Serializable]
    public class MultiInteractorActivatableState
    {
        public ushort StateChangeNumber { get; set; }
        public bool IsActivated;
        public ushort MostRecentInteractingClientID;
    }
}