using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using ViRSE.Core.Shared;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.Core.VComponents
{
    [Serializable]
    public class ActivatableStateConfig : BaseStateConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Space(5)]
        [Title("Activation Settings", ApplyCondition = true)]
        [SerializeField] public UnityEvent OnActivate = new();

        [EndGroup]
        [SerializeField] public UnityEvent OnDeactivate = new();
    }

    public class SingleInteractorActivatableStateModule : BaseStateModule, ISingleInteractorActivatableStateModule
    {
        #region plugin interfaces
        UnityEvent ISingleInteractorActivatableStateModule.OnActivate => _config.OnActivate;
        UnityEvent ISingleInteractorActivatableStateModule.OnDeactivate => _config.OnDeactivate;
        bool ISingleInteractorActivatableStateModule.IsActivated { get => _state.IsActivated; set => ReceiveNewActivationStateFromCustomer(value); }
        InteractorID ISingleInteractorActivatableStateModule.CurrentInteractor => _state.CurrentInteractor;
        #endregion

        private SingleInteractorActivatableState _state => (SingleInteractorActivatableState)State;

        private ActivatableStateConfig _config => (ActivatableStateConfig)Config;

        public SingleInteractorActivatableStateModule(ViRSESerializable state, BaseStateConfig config, string goName) : base(state, config, goName) { }

        public event Action OnProgrammaticStateChangeFromPlugin;

        //public SingleInteractorActivatableStateModule(ActivatableStateConfig config, ViRSESerializable state, string goName) : base(state, config, goName)
        //{
        //}

        private void ReceiveNewActivationStateFromCustomer(bool newIsActivated)
        {
            if (newIsActivated != _state.IsActivated)
                InvertState(null);

            OnProgrammaticStateChangeFromPlugin?.Invoke();
        }

        public void InvertState(InteractorID interactorID)
        {
            _state.IsActivated = !_state.IsActivated;
            _state.CurrentInteractor = _state.IsActivated ? interactorID : null;
            _state.StateChangeNumber++;

            if (_state.IsActivated)
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
                Debug.Log($"Error when emitting OnActivate from {GOName} \n{e.Message}\n{e.StackTrace}");
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
                Debug.Log($"Error when emitting OnDeactivate from {GOName} \n{e.Message}\n{e.StackTrace}");
            }
        }

        protected override void UpdateBytes(byte[] newBytes)
        {
            bool oldIsActivated = _state.IsActivated;
            State.Bytes = newBytes;
            //State = new(receivedStateAsBytes);

            if (_state.IsActivated && !oldIsActivated)
                InvokeCustomerOnActivateEvent();
            else if (!_state.IsActivated && oldIsActivated)
                InvokeCustomerOnDeactivateEvent();
        }
    }

    [Serializable]
    public class SingleInteractorActivatableState : ViRSESerializable
    {
        public ushort StateChangeNumber { get; set; }
        public bool IsActivated { get; set; }
        public InteractorID CurrentInteractor { get; set; }

        public SingleInteractorActivatableState()
        {
            StateChangeNumber = 0;
            IsActivated = false;
            CurrentInteractor = null;
        }

        //public SingleInteractorActivatableState(byte[] data) : base(data) { }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(StateChangeNumber);
            writer.Write(IsActivated);

            writer.Write(CurrentInteractor != null);
            if (CurrentInteractor != null)
            {
                writer.Write((ushort)CurrentInteractor.ClientID);
                writer.Write((ushort)CurrentInteractor.InteractorType);
            }

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            StateChangeNumber = reader.ReadUInt16();
            IsActivated = reader.ReadBoolean();

            bool someoneInteracting = reader.ReadBoolean();
            if (someoneInteracting)
            {
                CurrentInteractor = new(
                    clientID: reader.ReadUInt16(), 
                    interactorType: (InteractorType)reader.ReadUInt16());
            }
            else
            {
                CurrentInteractor = null;
            }
        }
    }
}

/*
 * TODO - we want both the interaction module and the sync module to be able to change state directly?
 * mmm, that might violage SR though... maybe having the VC orchestrate is better?
 * Then again, it's simpler overall if the interaction and network modules DO change state 
 * Maybe we can just pass the entire StateModule directly??
 * 
 */