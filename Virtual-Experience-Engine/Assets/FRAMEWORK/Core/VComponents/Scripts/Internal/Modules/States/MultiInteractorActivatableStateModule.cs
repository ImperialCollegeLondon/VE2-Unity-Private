using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager;
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
        public bool IsActivated { get => _state.IsActivated; set => HandleExternalActivation(value); }
        public ushort MostRecentInteractingClientID => _mostRecentInteractingInteractorID.ClientID;
        public List<ushort> CurrentlyInteractingClientIDs => _state.GetInteractingClientIDs();

        private MultiInteractorActivatableState _state = null;
        private HoldActivatableStateConfig _config = null;

        private InteractorID _mostRecentInteractingInteractorID = new(ushort.MaxValue, InteractorType.None);

        public MultiInteractorActivatableStateModule(MultiInteractorActivatableState state, HoldActivatableStateConfig config)
        {
            _state = state;
            _config = config;
        }

        private void HandleExternalActivation(bool newIsActivated)
        {
            SetState(new(ushort.MaxValue, InteractorType.None), newIsActivated);
        }

        public void SetState(InteractorID interactorId, bool activationState)
        {
            _state.IsActivated = activationState;

            _state.StateChangeNumber++;

            if (_state.IsActivated)
            {
                if (interactorId.ClientID != ushort.MaxValue)
                    _state.interactingClientIds.Add(interactorId);

                InvokeCustomerOnActivateEvent();
            }
            else
            {
                if (interactorId.ClientID != ushort.MaxValue && _state.interactingClientIds.Contains(interactorId))
                    _state.interactingClientIds.Remove(interactorId);

                InvokeCustomerOnDeactivateEvent();
            }

            if (_state.interactingClientIds.Count > 0)
                _mostRecentInteractingInteractorID = _state.interactingClientIds.Last();
            else
                _mostRecentInteractingInteractorID = interactorId;
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
    public class MultiInteractorActivatableState
    {
        public ushort StateChangeNumber { get; set; }
        public bool IsActivated { get; set; }
        public HashSet<InteractorID> interactingClientIds = new HashSet<InteractorID>();

        public List<ushort> GetInteractingClientIDs()
        {
            List<ushort> clientIDs = new List<ushort>();

            foreach (InteractorID id in interactingClientIds)
            {
                clientIDs.Add(id.ClientID);
            }

            return clientIDs;
        }
    }
}