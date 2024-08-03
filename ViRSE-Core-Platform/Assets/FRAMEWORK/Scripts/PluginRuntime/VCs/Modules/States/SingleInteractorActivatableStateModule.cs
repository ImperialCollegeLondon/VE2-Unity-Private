using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace ViRSE.PluginRuntime.VComponents
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
            State = new();
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
            State.StateChangeNumber++;

            if (State.IsActivated)
                InvokeCustomerOnActivateEvent();
            else
                InvokeCustomerOnDeactivateEvent();
        }

        public void UpdateToReceivedNetworkState(byte[] receivedStateAsBytes) //TODO, put in some interface? or abstract superclass?
        {
            bool oldIsActivated = State.IsActivated;
            State.Bytes = receivedStateAsBytes;

            if (State.IsActivated && !oldIsActivated)
                InvokeCustomerOnActivateEvent();
            else if (!State.IsActivated && oldIsActivated)
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

    public class SingleInteractorActivatableState : VSerializable
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