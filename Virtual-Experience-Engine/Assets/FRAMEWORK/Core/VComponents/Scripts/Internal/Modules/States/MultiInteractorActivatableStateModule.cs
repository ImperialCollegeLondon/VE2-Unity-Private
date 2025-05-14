using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class HoldActivatableStateConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Activation Settings", ApplyCondition = true)]
        [SerializeField, IgnoreParent] internal MultiInteractorActivatableStateDebug InspectorDebug = new();

        [SerializeField] internal UnityEvent OnActivate = new();

        [EndGroup(Order = 1)]
        [SpaceArea(spaceAfter: 10, Order = -1), SerializeField] internal UnityEvent OnDeactivate = new();

        [BeginGroup(Style = GroupStyle.Round, ApplyCondition = true)]
        [Title("Transmission Settings", ApplyCondition = true)]
        [EndGroup(ApplyCondition = true, Order = 5)]
        [SerializeField] internal bool IsNetworked = true;
    }

    [Serializable]
    internal class MultiInteractorActivatableStateDebug
    {
        [Title("Debug Output", ApplyCondition = true, Order = 50), SerializeField, ShowDisabledIf(nameof(IsInPlayMode), true)] public bool IsActivated = false;
        [InspectorName("Client IDs"), SerializeField, ShowDisabledIf(nameof(IsInPlayMode), true), SpaceArea(spaceAfter: 15, ApplyCondition = true)] public List<ushort> ClientIDs = new();

        protected bool IsInPlayMode => Application.isPlaying;
    }

    internal class MultiInteractorActivatableStateModule : IMultiInteractorActivatableStateModule
    {
        public UnityEvent OnActivate => _config.OnActivate;
        public UnityEvent OnDeactivate => _config.OnDeactivate;
        public bool IsActivated => _state.IsActivated;
        public IClientIDWrapper MostRecentInteractingClientID => _mostRecentInteractingInteractorID.ClientID == ushort.MaxValue ? null : 
            new ClientIDWrapper(_mostRecentInteractingInteractorID.ClientID, _mostRecentInteractingInteractorID.ClientID == _localClientIdWrapper.Value);
            
        public List<IClientIDWrapper> CurrentlyInteractingClientIDs {
            get 
            {
                List<IClientIDWrapper> clientIDs = new List<IClientIDWrapper>();
                foreach (InteractorID id in _state.InteractingInteractorIds)
                    clientIDs.Add(new ClientIDWrapper(id.ClientID, id.ClientID == _localClientIdWrapper.Value));
                    
                return clientIDs;
            }
        }

        private MultiInteractorActivatableState _state = null;
        private HoldActivatableStateConfig _config = null;
        private string _id = null;

        private InteractorID _mostRecentInteractingInteractorID = new(ushort.MaxValue, InteractorType.None);

        private readonly IClientIDWrapper _localClientIdWrapper;

        public MultiInteractorActivatableStateModule(MultiInteractorActivatableState state, HoldActivatableStateConfig config, string id, IClientIDWrapper localClientIdWrapper)
        {
            _state = state;
            _config = config;
            _id = id;
            _localClientIdWrapper = localClientIdWrapper;
        }

        public void AddInteractorToState(InteractorID interactorId)
        {
            _state.InteractingInteractorIds.Add(interactorId);
            _mostRecentInteractingInteractorID = interactorId;

            if (_state.InteractingInteractorIds.Count > 0 && !_state.IsActivated)
            {
                _state.IsActivated = true;
                InvokeCustomerOnActivateEvent();
            }
            else if (_state.IsActivated && _state.InteractingInteractorIds.Count == 0)
            {
                _state.IsActivated = false;
                InvokeCustomerOnDeactivateEvent();
            }

            _config.InspectorDebug.IsActivated = _state.IsActivated;
            _config.InspectorDebug.ClientIDs = _state.GetInteractingClientIDs();
        }

        public void RemoveInteractorFromState(InteractorID interactorId)
        {
            _state.InteractingInteractorIds.Remove(interactorId);
            _mostRecentInteractingInteractorID = interactorId;

            if (_state.InteractingInteractorIds.Count > 0 && !_state.IsActivated)
            {
                _state.IsActivated = true;
                InvokeCustomerOnActivateEvent();
            }
            else if (_state.IsActivated && _state.InteractingInteractorIds.Count == 0)
            {
                _state.IsActivated = false;
                InvokeCustomerOnDeactivateEvent();
            }

            _config.InspectorDebug.IsActivated = _state.IsActivated;
            _config.InspectorDebug.ClientIDs = _state.GetInteractingClientIDs();
        }

        private void InvokeCustomerOnActivateEvent()
        {
            try
            {
                _config.OnActivate?.Invoke();
            }
            catch (Exception e)
            {
                Debug.Log($"Error when emitting OnActivate from activatable with ID {_mostRecentInteractingInteractorID.ClientID} \n{e.Message}\n{e.StackTrace}");
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
                Debug.Log($"Error when emitting OnDeactivate from activatable with ID {_mostRecentInteractingInteractorID.ClientID} \n{e.Message}\n{e.StackTrace}");
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
    internal class MultiInteractorActivatableState
    {
        public bool IsActivated { get; set; }
        public HashSet<InteractorID> InteractingInteractorIds { get; set; }

        public MultiInteractorActivatableState()
        {
            IsActivated = false;
            InteractingInteractorIds = new HashSet<InteractorID>();
        }

        public List<ushort> GetInteractingClientIDs()
        {
            List<ushort> clientIDs = new List<ushort>();

            foreach (InteractorID id in InteractingInteractorIds)
            {
                clientIDs.Add(id.ClientID);
            }

            return clientIDs;
        }
    }
}