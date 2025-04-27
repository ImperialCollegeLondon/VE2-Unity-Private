using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using VE2.Core.VComponents.API;
using static VE2.Core.Common.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class ToggleActivatableStateConfig : BaseWorldStateConfig
    {
        [Title("Activation State", ApplyCondition = true, Order = -50)]
        [BeginGroup(Style = GroupStyle.Round, ApplyCondition = false)]
        [SerializeField, IgnoreParent] internal SingleInteractorActivatableStateDebug InspectorDebug = new();

        [SerializeField] public UnityEvent OnActivate = new();
        [SpaceArea(spaceAfter: 10, Order = -1), SerializeField] public UnityEvent OnDeactivate = new();

        [SpaceArea(spaceAfter: 10, Order = -2), SerializeField] public bool UseActivationGroup = false;
        [EndGroup(ApplyCondition = false, Order = 50)]
        [SpaceArea(spaceAfter: 5, Order = -3), ShowIf("UseActivationGroup",true), DisableInPlayMode(), SerializeField] public string ActivationGroupID = "None";
    }

    [Serializable]
    internal class SingleInteractorActivatableStateDebug
    {
        [Title("Debug Output", ApplyCondition = true, Order = 50), SerializeField, ShowDisabledIf(nameof(IsInPlayMode), true)] public bool IsActivated = false;
        [SerializeField, ShowDisabledIf(nameof(IsInPlayMode), true)] public ushort ClientID = ushort.MaxValue;
        [EditorButton(nameof(HandleDebugUpdateStatePressed), "Update State", activityType: ButtonActivityType.OnPlayMode, ApplyCondition = true, Order = 10), SpaceArea(spaceAfter:15, ApplyCondition = true)]
        [Title("Debug Input", ApplyCondition = true), SerializeField, HideIf(nameof(IsInPlayMode), false)] private bool _newState = false;

        public void HandleDebugUpdateStatePressed() 
        {
            Debug.Log($"Debug button pressed");
            OnDebugUpdateStatePressed?.Invoke(_newState);
        }
        internal event Action<bool> OnDebugUpdateStatePressed;

        protected bool IsInPlayMode => Application.isPlaying;
    }

    internal class SingleInteractorActivatableStateModule : BaseWorldStateModule, ISingleInteractorActivatableStateModule
    {
        public UnityEvent OnActivate => _config.OnActivate;
        public UnityEvent OnDeactivate => _config.OnDeactivate;
        public bool IsActivated { get => _state.IsActivated; }
        public ushort MostRecentInteractingClientID => _state.MostRecentInteractingClientID;

        public void Activate() => SetActivated(true);
        public void Deactivate() => SetActivated(false);

        public void SetActivated(bool newIsActivated)
        {
            if (newIsActivated != _state.IsActivated)
                ToggleActivatableState(ushort.MaxValue);
            else
                Debug.LogWarning($"Tried to set activated state on {ID} to {newIsActivated} but state is already {_state.IsActivated}");
        }

        private string _activationGroupID = "None";
        private bool _isInActivationGroup = false;     
        private SingleInteractorActivatableState _state => (SingleInteractorActivatableState)State;
        private ToggleActivatableStateConfig _config => (ToggleActivatableStateConfig)Config;
        

        private readonly ActivatableGroupsContainer _activatableGroupsContainer;

        public SingleInteractorActivatableStateModule(VE2Serializable state, BaseWorldStateConfig config, string id, IWorldStateSyncService worldStateSyncService, ActivatableGroupsContainer activatableGroupsContainer) : base(state, config, id, worldStateSyncService)
        {
            _activationGroupID = _config.ActivationGroupID;
            _activatableGroupsContainer = activatableGroupsContainer;
            if (_activationGroupID != "None")
            {
                _activatableGroupsContainer.RegisterActivatable(_activationGroupID, this);
                _isInActivationGroup = true;
            }
            else
            {
                _isInActivationGroup = false;
            }

            _config.InspectorDebug.OnDebugUpdateStatePressed += (bool newState) => SetActivated(newState);
        }

        private void HandleExternalActivation(bool newIsActivated)
        {
            if (newIsActivated != _state.IsActivated)
                ToggleActivatableState(ushort.MaxValue);
            else
                Debug.LogWarning($"Tried to set activated state on {ID} to {newIsActivated} but state is already {_state.IsActivated}");
        }

        public void ToggleActivatableState(ushort clientID)
        {
            Debug.Log($"ToggleActivatableState called on {ID} with clientID {clientID}");
            if (_isInActivationGroup)
            {
                List<ISingleInteractorActivatableStateModule> singleInteractorActivatableStateModules = _activatableGroupsContainer.GetSingleInteractorActivatableStateModule(_activationGroupID);

                foreach (ISingleInteractorActivatableStateModule activatable in singleInteractorActivatableStateModules)
                {
                    if (activatable != this && activatable.IsActivated)
                        activatable.Deactivate();
                }

                InvertState(clientID);
            }
            else
            {
                InvertState(clientID);
            }
        }

        public void SetHoldActivatableState(ushort clientID, bool isHolding)
        {
            if (isHolding)
            {
                // If this module belongs to an activation group, deactivate others.
                if (_isInActivationGroup)
                {
                    List<ISingleInteractorActivatableStateModule> groupModules = _activatableGroupsContainer.GetSingleInteractorActivatableStateModule(_activationGroupID);
                    foreach (ISingleInteractorActivatableStateModule module in groupModules)
                    {
                        if (module != this && module.IsActivated)
                            module.Deactivate();
                    }
                }
                // Activate this module if not already active.
                if (!_state.IsActivated)
                {
                    UpdateActivationState(clientID, true);
                }
            }
            else
            {
                // When the hold is released, deactivate if currently active.
                if (_state.IsActivated)
                {
                    UpdateActivationState(clientID, false);
                }
            }
        }
        public void InvertState(ushort clientID)
        {
            Debug.Log($"InvertState called on {ID} with clientID {clientID}");
            UpdateActivationState(clientID, !_state.IsActivated);
        }
        private void UpdateActivationState(ushort clientID, bool newState)
        {
            // Only update if the state is actually changing.
            if (_state.IsActivated == newState)
                return;

            _state.IsActivated = newState;
            _config.InspectorDebug.IsActivated = newState;
            _config.InspectorDebug.ClientID = clientID;

            if (clientID != ushort.MaxValue)
                _state.MostRecentInteractingClientID = clientID;

            _state.StateChangeNumber++;

            if (newState)
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
                Debug.Log($"Error when emitting OnActivate from activatable with ID {ID} \n{e.Message}\n{e.StackTrace}");
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
                Debug.Log($"Error when emitting OnDeactivate from activatable with ID {ID} \n{e.Message}\n{e.StackTrace}");
            }
        }

        protected override void UpdateBytes(byte[] newBytes)
        {
            bool oldIsActivated = _state.IsActivated;
            State.Bytes = newBytes;

            if (_state.IsActivated && !oldIsActivated)
                InvokeCustomerOnActivateEvent();
            else if (!_state.IsActivated && oldIsActivated)
                InvokeCustomerOnDeactivateEvent();
        }

        public override void TearDown()
        {   
            base.TearDown();

            if (_isInActivationGroup)
            {
                _activatableGroupsContainer.DeregisterActivatable(_activationGroupID, this);
            }
        }
    }

    [Serializable]
    public class SingleInteractorActivatableState : VE2Serializable
    {
        public ushort StateChangeNumber { get; set; }
        public bool IsActivated { get; set; }
        public ushort MostRecentInteractingClientID { get; set; }

        public SingleInteractorActivatableState()
        {
            StateChangeNumber = 0;
            IsActivated = false;
            MostRecentInteractingClientID = ushort.MaxValue;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(StateChangeNumber);
            writer.Write(IsActivated);
            writer.Write(MostRecentInteractingClientID);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            StateChangeNumber = reader.ReadUInt16();
            IsActivated = reader.ReadBoolean();
            MostRecentInteractingClientID = reader.ReadUInt16();
        }
    }
}
