using System;
using UnityEngine;
using UnityEngine.Events;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class MultiInteractorActivatableStateModule : IMultiInteractorActivatableStateModule
    {
        public UnityEvent OnActivate => _config.OnActivate;
        public UnityEvent OnDeactivate => _config.OnDeactivate;
        public bool IsActivated { get => _state.IsActivated; set => HandleExternalActivation(value); }
        public ushort MostRecentInteractingClientID => _state.MostRecentInteractingClientID;

        private MultiInteractorActivatableState _state = null;
        private ActivatableStateConfig _config = null;

        public MultiInteractorActivatableStateModule(MultiInteractorActivatableState state, BaseWorldStateConfig config, string id)
        {
            _state = (MultiInteractorActivatableState)state;
            _config = (ActivatableStateConfig)config;
        }

        private void HandleExternalActivation(bool newIsActivated)
        {
            if (newIsActivated != _state.IsActivated)
                InvertState(ushort.MaxValue);
        }

        public void InvertState(ushort clientID)
        {

        }

        private void InvokeCustomerOnActivateEvent()
        {

        }

        private void InvokeCustomerOnDeactivateEvent()
        {

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