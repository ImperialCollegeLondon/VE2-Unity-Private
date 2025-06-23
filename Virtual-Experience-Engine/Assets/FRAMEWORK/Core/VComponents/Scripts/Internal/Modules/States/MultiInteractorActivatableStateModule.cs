using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Org.BouncyCastle.Asn1.Misc;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Shared;
using static VE2.Common.Shared.CommonSerializables;

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
    }

    [Serializable]
    internal class HoldActivatablePlayerSyncIndicator
    {
        [BeginGroup(Style = GroupStyle.Round, ApplyCondition = true)]
        [Title("Sync Settings", ApplyCondition = true)]
        [EndGroup(ApplyCondition = true, Order = 5)]
        [Help("This component is syncronised through the player, and so will use the player's sync settings.")]
        [SerializeField, DisableInPlayMode] public bool IsNetworked = true;

        public HoldActivatablePlayerSyncIndicator(bool isNetworked)
        {
            IsNetworked = isNetworked;
        }

        public HoldActivatablePlayerSyncIndicator() { }

        //Note, if we want this to be configurable at runtime, we'll need to add an event for the player to listen to 
        //The player will need to remove the activatable from its list when becoming not networked, and vice versa
    }

    [Serializable]
    internal class MultiInteractorActivatableStateDebug
    {
        [Title("Debug Output", ApplyCondition = true, Order = 50), SerializeField, ShowDisabledIf(nameof(IsInPlayMode), true)] public bool IsActivated = false;
        [InspectorName("Client IDs"), SerializeField, ShowDisabledIf(nameof(IsInPlayMode), true), SpaceArea(spaceAfter: 15, ApplyCondition = true)] public List<ushort> ClientIDs = new();

        protected bool IsInPlayMode => Application.isPlaying;
    }

    internal class MultiInteractorActivatableStateModule : BaseWorldStateModule, IMultiInteractorActivatableStateModule
    {
        public UnityEvent OnActivate => _config.OnActivate;
        public UnityEvent OnDeactivate => _config.OnDeactivate;
        public bool IsActivated => _localState.IsActivated || _sycnedState.IsProgrammaticallyActivated;

        public void ToggleAlwaysActivated(bool toggle)
        {
            bool wasActivated = IsActivated;

            _sycnedState.StateChangeNumber++;
            _sycnedState.IsProgrammaticallyActivated = toggle;

            if (IsActivated && ! wasActivated)
                InvokeCustomerOnActivateEvent();
            else if (!IsActivated && wasActivated)
                InvokeCustomerOnDeactivateEvent();
        }

        public IClientIDWrapper MostRecentInteractingClientID => _mostRecentInteractingInteractorID.ClientID == ushort.MaxValue ? null :
            new ClientIDWrapper(_mostRecentInteractingInteractorID.ClientID, _mostRecentInteractingInteractorID.ClientID == _localClientIdWrapper.Value);

        public List<IClientIDWrapper> CurrentlyInteractingClientIDs
        {
            get
            {
                List<IClientIDWrapper> clientIDs = new List<IClientIDWrapper>();
                foreach (InteractorID id in _localState.InteractingInteractorIds)
                    clientIDs.Add(new ClientIDWrapper(id.ClientID, id.ClientID == _localClientIdWrapper.Value));

                return clientIDs;
            }
        }

        private InteractorID _mostRecentInteractingInteractorID = new(ushort.MaxValue, InteractorType.None);
        // private bool IsInteractorIDProgrammatic(InteractorID interactorId) => interactorId.ClientID == PROGRAMMATIC_INTERACTOR_ID && interactorId.InteractorType == PROGRAMMATIC_INTERACTOR_TYPE;
        // private const ushort PROGRAMMATIC_INTERACTOR_ID = ushort.MaxValue;
        // //TODO: Can probably remove this whole programmatic interactor thing, it can just live in the remote state instead
        // private const InteractorType PROGRAMMATIC_INTERACTOR_TYPE = InteractorType.None;

        private readonly MultiInteractorActivatableLocalState _localState = new();
        private readonly MultiInteractorActivatableSyncedState _sycnedState;
        private readonly HoldActivatableStateConfig _config;
        private readonly string _id;
        private readonly IClientIDWrapper _localClientIdWrapper;

        public MultiInteractorActivatableStateModule(MultiInteractorActivatableSyncedState remoteState, HoldActivatableStateConfig config,
            string id, IClientIDWrapper localClientIdWrapper, WorldStateSyncConfig syncConfig, IWorldStateSyncableContainer worldStateSyncableContainer)
            : base(remoteState, syncConfig, id, worldStateSyncableContainer)
        {
            _config = config;
            _id = id;
            _localClientIdWrapper = localClientIdWrapper;
            _sycnedState = remoteState;
        }

        public void AddInteractorToState(InteractorID interactorId)
        {
            if (_localState.InteractingInteractorIds.Contains(interactorId))
            {
                Debug.LogWarning($"Tried to add interactor ID {interactorId.ClientID} to state of activatable with ID {_id} but it is already in the state");
                return;
            }

            _localState.InteractingInteractorIds.Add(interactorId);
            _mostRecentInteractingInteractorID = interactorId;

            if (_localState.InteractingInteractorIds.Count == 1)
                InvokeCustomerOnActivateEvent();

            _config.InspectorDebug.IsActivated = _localState.IsActivated;
            _config.InspectorDebug.ClientIDs = _localState.GetInteractingClientIDs();
        }

        public void RemoveInteractorFromState(InteractorID interactorId)
        {
            if (!_localState.InteractingInteractorIds.Contains(interactorId))
            {
                Debug.LogWarning($"Tried to remove interactor ID {interactorId.ClientID} from state of activatable with ID {_id} but it is not in the state");
                return;
            }

            _localState.InteractingInteractorIds.Remove(interactorId);
            _mostRecentInteractingInteractorID = interactorId;

            if (_localState.InteractingInteractorIds.Count == 0)
                InvokeCustomerOnDeactivateEvent();

            _config.InspectorDebug.IsActivated = _localState.IsActivated;
            _config.InspectorDebug.ClientIDs = _localState.GetInteractingClientIDs();
        }

        private void InvokeCustomerOnActivateEvent()
        {
            try
            {
                _config.OnActivate?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error when emitting OnActivate from activatable with ID {_id} \n{e.Message}\n{e.StackTrace}");
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
                Debug.LogError($"Error when emitting OnDeactivate from activatable with ID {_id} \n{e.Message}\n{e.StackTrace}");
            }
        }

        protected override void UpdateBytes(byte[] newBytes)
        {
            MultiInteractorActivatableSyncedState receivedRemoteState = new(newBytes);
            ToggleAlwaysActivated(receivedRemoteState.IsProgrammaticallyActivated);
        }
    }

    [Serializable]
    internal class MultiInteractorActivatableLocalState
    {
        public bool IsActivated => InteractingInteractorIds.Count > 0;
        public HashSet<InteractorID> InteractingInteractorIds { get; set; }

        public MultiInteractorActivatableLocalState()
        {
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

    [Serializable]
    internal class MultiInteractorActivatableSyncedState : VE2Serializable
    {
        public ushort StateChangeNumber { get; set; } = 0;
        public bool IsProgrammaticallyActivated { get; set; } = false;

        public MultiInteractorActivatableSyncedState() {}

        public MultiInteractorActivatableSyncedState(byte[] bytes)
        {
            PopulateFromBytes(bytes);
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(StateChangeNumber);
            writer.Write(IsProgrammaticallyActivated);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            StateChangeNumber = reader.ReadUInt16();
            IsProgrammaticallyActivated = reader.ReadBoolean();
        }
    }
}