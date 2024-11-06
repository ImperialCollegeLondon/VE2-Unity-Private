using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common;
using VE2.Core.Common;
using VE2.Core.VComponents.NonInteractableInterfaces;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    public class FreeGrabbableStateConfig : BaseStateConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Grab State Settings", ApplyCondition = true)]
        [SerializeField] public UnityEvent OnGrab = new();

        [EndGroup(Order = 1)]
        [SpaceArea(spaceAfter: 10, Order = -1), SerializeField] public UnityEvent OnDrop = new();
    }

    internal class FreeGrabbableStateModule : BaseWorldStateModule, IFreeGrabbableStateModule
    {
        public UnityEvent OnGrab => _config.OnGrab;

        public UnityEvent OnDrop => _config.OnDrop;

        public event Action<InteractorID> OnGrabInternal;
        public event Action<InteractorID> OnDropInternal;
        public bool IsGrabbed { get => _state.IsGrabbed; private set => _state.IsGrabbed = value; }
        public ushort MostRecentInteractingClientID => _state.MostRecentInteractingInteractorID.ClientID;

        private FreeGrabbableState _state => (FreeGrabbableState)State;
        private FreeGrabbableStateConfig _config => (FreeGrabbableStateConfig)Config;



        public FreeGrabbableStateModule(VE2Serializable state, BaseStateConfig config, string id, WorldStateModulesContainer worldStateModulesContainer) : base(state, config, id, worldStateModulesContainer) { }

        public event Action OnProgrammaticStateChangeFromPlugin;


        public void SetGrabbed(InteractorID interactorID)
        {
            if (IsGrabbed)
                return;

            _state.MostRecentInteractingInteractorID = interactorID;
                
            _state.StateChangeNumber++;

            OnGrabInternal?.Invoke(interactorID);
        }

        public void SetDropped(InteractorID interactorID)
        {
            if (!IsGrabbed)
                return;

            if (interactorID.ClientID != ushort.MaxValue)
                _state.MostRecentInteractingInteractorID = interactorID;

            _state.StateChangeNumber++;

            OnDropInternal?.Invoke(interactorID);
        }
        //private void InvokeCustomerOnActivateEvent()
        //{
        //    try
        //    {
        //        _config.OnActivate?.Invoke();
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.Log($"Error when emitting OnGrab from activatable with ID {ID} \n{e.Message}\n{e.StackTrace}");
        //    }
        //}

        //private void InvokeCustomerOnDeactivateEvent()
        //{
        //    try
        //    {
        //        _config.OnDeactivate?.Invoke();
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.Log($"Error when emitting OnDrop from activatable with ID {ID} \n{e.Message}\n{e.StackTrace}");
        //    }
        //}

        protected override void UpdateBytes(byte[] newBytes)
        {
            FreeGrabbableState receiveState = new FreeGrabbableState(newBytes);

            if (receiveState.IsGrabbed)
            {
                SetGrabbed(receiveState.MostRecentInteractingInteractorID);
            }
            else
            {
                SetDropped(receiveState.MostRecentInteractingInteractorID);
            }
        }
    }

    [Serializable]
    public class FreeGrabbableState : VE2Serializable
    {
        public ushort StateChangeNumber { get; set; }
        public bool IsGrabbed { get; set; }
        public InteractorID MostRecentInteractingInteractorID { get; set; }

        public FreeGrabbableState()
        {
            StateChangeNumber = 0;
            IsGrabbed = false;
            MostRecentInteractingInteractorID = new InteractorID(ushort.MaxValue,InteractorType.None);
        }
        public FreeGrabbableState(byte[] bytes): base(bytes) { }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(StateChangeNumber);
            writer.Write(IsGrabbed);

            byte[] bytes = MostRecentInteractingInteractorID.Bytes;
            writer.Write((ushort)bytes.Length);
            writer.Write(bytes);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            StateChangeNumber = reader.ReadUInt16();
            IsGrabbed = reader.ReadBoolean();

            ushort mostRecentInteractingInteractorIDLength = reader.ReadUInt16();
            MostRecentInteractingInteractorID = new InteractorID(reader.ReadBytes(mostRecentInteractingInteractorIDLength));
        }
    }
}
