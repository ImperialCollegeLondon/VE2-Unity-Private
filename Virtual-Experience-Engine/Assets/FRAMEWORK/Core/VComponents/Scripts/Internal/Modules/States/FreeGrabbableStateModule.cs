using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using VE2.Core.VComponents.API;
using static VE2.Core.Common.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class FreeGrabbableStateConfig : BaseWorldStateConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Grab State Settings", ApplyCondition = true)]
        [SerializeField] public UnityEvent OnGrab = new();

        [EndGroup(Order = 1)]
        [SpaceArea(spaceAfter: 10, Order = -1), SerializeField] public UnityEvent OnDrop = new();
    }

    internal class FreeGrabbableStateModule : BaseWorldStateModule, IFreeGrabbableStateModule
    {
        #region Interfaces
        public UnityEvent OnGrab => _config.OnGrab;
        public UnityEvent OnDrop => _config.OnDrop;
        public bool IsGrabbed { get => _state.IsGrabbed; private set => _state.IsGrabbed = value; }
        public ushort MostRecentInteractingClientID => _state.MostRecentInteractingInteractorID.ClientID;
        #endregion

        private FreeGrabbableState _state => (FreeGrabbableState)State;
        private FreeGrabbableStateConfig _config => (FreeGrabbableStateConfig)Config;

        private readonly InteractorContainer _interactorContainer;
        private readonly IRangedFreeGrabInteractionModule _rangedGrabInteractionModule;

        internal IInteractor CurrentGrabbingInteractor { get; private set; }
        internal event Action OnGrabConfirmed;
        internal event Action OnDropConfirmed;

        public FreeGrabbableStateModule(VE2Serializable state, BaseWorldStateConfig config, string id, 
            IWorldStateSyncService worldStateSyncService, InteractorContainer interactorContainer, IRangedFreeGrabInteractionModule rangedGrabInteractionModule) : 
            base(state, config, id, worldStateSyncService)
        {
            _interactorContainer = interactorContainer;
            _rangedGrabInteractionModule = rangedGrabInteractionModule;
        }

        public void SetGrabbed(InteractorID interactorID)
        {
            if (IsGrabbed)
                return;

            if (_interactorContainer.Interactors.TryGetValue(interactorID.ToString(), out IInteractor interactor))
            {
                CurrentGrabbingInteractor = interactor;
                _state.IsGrabbed = true;
                _state.MostRecentInteractingInteractorID = interactorID;
                _state.StateChangeNumber++;

                interactor.ConfirmGrab(_rangedGrabInteractionModule);
                OnGrabConfirmed?.Invoke();

                try
                {
                    _config.OnGrab?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.Log($"Error when emitting OnLocalInteractorGrab from activatable with ID {ID} \n{e.Message}\n{e.StackTrace}");
                }
            }
            else
            {
                Debug.LogError($"Could not find Interactor with {interactorID.ClientID} and {interactorID.InteractorType}");
            }
        }

        public void SetDropped(InteractorID interactorID)
        {
            if (!IsGrabbed)
                return;

            //Different validation to SetGrabbed. The interactor may have been destroyed (and is thus no longer present), but we still want to set the state to dropped
            CurrentGrabbingInteractor = null;
            _state.IsGrabbed = false;
            _state.StateChangeNumber++;

            if (_interactorContainer.Interactors.TryGetValue(interactorID.ToString(), out IInteractor interactor))
                interactor.ConfirmDrop();

            OnDropConfirmed?.Invoke();

            try
            {
                _config.OnDrop?.Invoke();
            }
            catch (Exception e)
            {
                Debug.Log($"Error when emitting OnLocalInteractorDrop from activatable with ID {ID} \n{e.Message}\n{e.StackTrace}");
            }
        }

        protected override void UpdateBytes(byte[] newBytes)
        {
            FreeGrabbableState receiveState = new(newBytes);

            if (receiveState.IsGrabbed)
            {
                SetGrabbed(receiveState.MostRecentInteractingInteractorID);
            }
            else
            {
                SetDropped(receiveState.MostRecentInteractingInteractorID);
            }
        }

        public override void HandleFixedUpdate()
        {
            base.HandleFixedUpdate();
            if (CurrentGrabbingInteractor == null && IsGrabbed)
            {
                SetDropped(_state.MostRecentInteractingInteractorID);
            }
        }
    }

    [Serializable]
    public class FreeGrabbableState : VE2Serializable
    {
        public ushort StateChangeNumber { get; set; }
        public bool IsGrabbed { get; set; }
        public InteractorID MostRecentInteractingInteractorID { get; set; } //TODO: - maybe this should just be a string? Maybe InteractorID doesn't need to be a VE2Serializable

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
