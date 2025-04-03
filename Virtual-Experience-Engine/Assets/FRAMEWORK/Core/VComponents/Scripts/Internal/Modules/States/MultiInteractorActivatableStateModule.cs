using System;
using System.Collections.Generic;
using System.Linq;
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
        [InspectorName("Client IDs"), SerializeField, ShowDisabledIf(nameof(IsInPlayMode), true), SpaceArea(spaceAfter:15, ApplyCondition = true)] public List<ushort> ClientIDs = new();

        protected bool IsInPlayMode => Application.isPlaying;
    }

    internal class MultiInteractorActivatableStateModule : IMultiInteractorActivatableStateModule
    {
        public UnityEvent OnActivate => _config.OnActivate;
        public UnityEvent OnDeactivate => _config.OnDeactivate;
        public bool IsActivated => _state.IsActivated;
        public ushort MostRecentInteractingClientID => _mostRecentInteractingInteractorID.ClientID;
        public List<ushort> CurrentlyInteractingClientIDs => _state.GetInteractingClientIDs();

        private MultiInteractorActivatableState _state = null;
        private HoldActivatableStateConfig _config = null;
        private string _id = null;

        private InteractorID _mostRecentInteractingInteractorID = new(ushort.MaxValue, InteractorType.None);

        public MultiInteractorActivatableStateModule(MultiInteractorActivatableState state, HoldActivatableStateConfig config, string id)
        {
            _state = state;
            _config = config;
            _id = id;
        }

        public void UpdateState(InteractorID interactorId)
        {
            if(_state.InteractingInteractorIds.Count > 0)
            {
                _state.IsActivated = true;
                InvokeCustomerOnActivateEvent();
            }
            else
            {
                _state.IsActivated = false;
                InvokeCustomerOnDeactivateEvent();
            }

            if (_state.InteractingInteractorIds.Count > 0)
                _mostRecentInteractingInteractorID = _state.InteractingInteractorIds.Last();
            else
                _mostRecentInteractingInteractorID = interactorId;

            _config.InspectorDebug.IsActivated = _state.IsActivated;
            _config.InspectorDebug.ClientIDs = _state.GetInteractingClientIDs();
        }

        public void AddInteractorToState(InteractorID interactorId)
        {
            _state.InteractingInteractorIds.Add(interactorId);
            UpdateState(interactorId);
        }

        public void RemoveInteractorFromState(InteractorID interactorId)
        {
            _state.InteractingInteractorIds.Remove(interactorId);
            UpdateState(interactorId);
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