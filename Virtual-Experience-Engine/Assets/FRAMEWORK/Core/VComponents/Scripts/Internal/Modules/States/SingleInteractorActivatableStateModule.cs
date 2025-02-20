using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using VE2.Core.VComponents.API;
using static VE2.Core.Common.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class ActivatableStateConfig : BaseWorldStateConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Activation Settings", ApplyCondition = true)]
        [SerializeField] public UnityEvent OnActivate = new();

        [EndGroup(Order = 1)]
        [SpaceArea(spaceAfter: 10, Order = -1), SerializeField] public UnityEvent OnDeactivate = new();
    }

    internal class SingleInteractorActivatableStateModule : BaseWorldStateModule, ISingleInteractorActivatableStateModule
    {
        public UnityEvent OnActivate => _config.OnActivate;
        public UnityEvent OnDeactivate => _config.OnDeactivate;
        public bool IsActivated { get => _state.IsActivated; set => HandleExternalActivation(value); }
        public ushort MostRecentInteractingClientID => _state.MostRecentInteractingClientID;

        private SingleInteractorActivatableState _state => (SingleInteractorActivatableState)State;
        private ActivatableStateConfig _config => (ActivatableStateConfig)Config;

        public SingleInteractorActivatableStateModule(VE2Serializable state, BaseWorldStateConfig config, string id, IWorldStateSyncService worldStateSyncService) : base(state, config, id, worldStateSyncService) { }

        private void HandleExternalActivation(bool newIsActivated)
        {
            if (newIsActivated != _state.IsActivated)
                InvertState(ushort.MaxValue);
        }

        public void InvertState(ushort clientID)
        {
            _state.IsActivated = !_state.IsActivated;

            if (clientID != ushort.MaxValue)
                _state.MostRecentInteractingClientID = clientID;
                
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
