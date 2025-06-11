using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.Core.Player.API;
using VE2.Core.VComponents.Shared;
using static VE2.Common.Shared.CommonSerializables;
using System.Collections.Generic;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class GrabbableStateConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Grab State Settings", ApplyCondition = true)]
        [SerializeField, IgnoreParent] internal GrabbableStateDebug InspectorDebug = new();
        [SerializeField] public UnityEvent OnGrab = new();
        [EndGroup]
        [SerializeField] public UnityEvent OnDrop = new();
    }

    [Serializable]
    internal class GrabbableStateDebug
    {
        [Title("Debug Output", ApplyCondition = true, Order = 50), SerializeField, ShowDisabledIf(nameof(IsInPlayMode), true)] public bool IsGrabbed = false;
        [InspectorName("Client IDs"), SerializeField, ShowDisabledIf(nameof(IsInPlayMode), true), SpaceArea(spaceAfter: 15, ApplyCondition = true)] public ushort ClientID = ushort.MaxValue;

        protected bool IsInPlayMode => Application.isPlaying;
    }

    internal class GrabbableStateModule : BaseWorldStateModule, IGrabbableStateModule
    {
        #region Interfaces
        public UnityEvent OnGrab => _grabbableStateConfig.OnGrab;
        public UnityEvent OnDrop => _grabbableStateConfig.OnDrop;
        public bool IsGrabbed { get => _state.IsGrabbed; private set => _state.IsGrabbed = value; }
        public bool IsLocalGrabbed => MostRecentInteractingClientID != null && IsGrabbed && MostRecentInteractingClientID.IsLocal;
        public IClientIDWrapper MostRecentInteractingClientID => _state.MostRecentInteractingInteractorID.ClientID == ushort.MaxValue ? null : 
            new ClientIDWrapper(_state.MostRecentInteractingInteractorID.ClientID, _state.MostRecentInteractingInteractorID.ClientID == _localClientIdWrapper.Value);
        #endregion

        private GrabbableState _state => (GrabbableState)State;

        private readonly GrabbableStateConfig _grabbableStateConfig;
        private readonly IGrabInteractablesContainer _grabInteractablesContainer;
        private readonly HandInteractorContainer _interactorContainer;
        private readonly IClientIDWrapper _localClientIdWrapper;

        internal IInteractor CurrentGrabbingInteractor { get; private set; }
        internal event Action<ushort> OnGrabConfirmed;
        internal event Action<ushort> OnDropConfirmed;

        internal event Action<Vector3> OnRequestTeleportRigidbody;

        private bool _grabIsLocked = false;

        public GrabbableStateModule(VE2Serializable state, GrabbableStateConfig grabbableStateConfig, WorldStateSyncConfig syncConfig, string id, IWorldStateSyncableContainer worldStateSyncableContainer, 
            IGrabInteractablesContainer grabInteractablesContainer, HandInteractorContainer interactorContainer, IClientIDWrapper localClientIdWrapper) :
            base(state, syncConfig, id, worldStateSyncableContainer)
        {
            _grabbableStateConfig = grabbableStateConfig;
            _grabInteractablesContainer = grabInteractablesContainer;
            _interactorContainer = interactorContainer;
            _localClientIdWrapper = localClientIdWrapper;
        }

        public void SetGrabbed(InteractorID interactorID)
        {
            if (IsGrabbed)
            {
                //If we're already grabbed, we can only be grabbed again by a different interactor of the same clientID
                if (_state.MostRecentInteractingInteractorID.ClientID != interactorID.ClientID || _state.MostRecentInteractingInteractorID.InteractorType == interactorID.InteractorType)
                    return;

                CurrentGrabbingInteractor.ConfirmDrop();

                if (_interactorContainer.Interactors.TryGetValue(interactorID.ToString(), out IInteractor interactor))
                {
                    CurrentGrabbingInteractor = interactor;
                    _state.MostRecentInteractingInteractorID = interactorID;
                    _state.StateChangeNumber++;
                    interactor.ConfirmGrab(ID);
                }
                else
                {
                    Debug.LogError($"Could not find Interactor with {interactorID.ClientID} and {interactorID.InteractorType}");
                }
            }
            else
            {
                if (_interactorContainer.Interactors.TryGetValue(interactorID.ToString(), out IInteractor interactor))
                {
                    CurrentGrabbingInteractor = interactor;
                    _state.IsGrabbed = true;
                    _state.MostRecentInteractingInteractorID = interactorID;
                    _state.StateChangeNumber++;

                    _grabbableStateConfig.InspectorDebug.IsGrabbed = true;
                    _grabbableStateConfig.InspectorDebug.ClientID = interactorID.ClientID;

                    interactor.ConfirmGrab(ID);
                    OnGrabConfirmed?.Invoke(interactorID.ClientID);

                    try
                    {
                        _grabbableStateConfig.OnGrab?.Invoke();
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
        }

        public void SetDropped(InteractorID interactorID)
        {
            if (!IsGrabbed || _grabIsLocked)
                return;

            //Different validation to SetGrabbed. The interactor may have been destroyed (and is thus no longer present), but we still want to set the state to dropped
            CurrentGrabbingInteractor = null;
            _state.IsGrabbed = false;
            _state.StateChangeNumber++;

            _grabbableStateConfig.InspectorDebug.IsGrabbed = true;
            _grabbableStateConfig.InspectorDebug.ClientID = interactorID.ClientID;

            if (_interactorContainer.Interactors.TryGetValue(interactorID.ToString(), out IInteractor interactor))
                interactor.ConfirmDrop();

            OnDropConfirmed?.Invoke(interactorID.ClientID);

            try
            {
                _grabbableStateConfig.OnDrop?.Invoke();
            }
            catch (Exception e)
            {
                Debug.Log($"Error when emitting OnLocalInteractorDrop from activatable with ID {ID} \n{e.Message}\n{e.StackTrace}");
            }
        }

        protected override void UpdateBytes(byte[] newBytes)
        {
            GrabbableState receiveState = new(newBytes);

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

        private bool DoLocalGrab(bool lockGrab, VRHandInteractorType handToGrabWith, bool forceDrop)
        {
            InteractorType interactorType = VE2API.Player.IsVRMode ? (InteractorType)handToGrabWith : InteractorType.Mouse2D;
            InteractorID interactorID = new(_localClientIdWrapper.Value, interactorType);

            if (!_interactorContainer.Interactors.TryGetValue(interactorID.ToString(), out IInteractor interactor))
            {
                Debug.LogError($"Could not find local interactor of type {interactorID.InteractorType}");
                return false;
            }

            if (interactor is ILocalInteractor interactorLocal)
            {
                // Either we're not grabbing, so should grab, or we're grabbing, should force drop, and this object is not already grabbed by the interactor
                // Using != as conditional XOR
                if (!interactorLocal.IsCurrentlyGrabbing || (forceDrop && !(_state.IsGrabbed && _state.MostRecentInteractingInteractorID == interactorID)))
                {
                    // Do grab
                    UnlockLocalGrab();

                    // Drop other item
                    if (interactorLocal.IsCurrentlyGrabbing)
                    {
                        string grabToDropID = interactorLocal.CurrentlyGrabbedGrabbableID;
                        if (_grabInteractablesContainer.GrabInteractables.TryGetValue(grabToDropID, out IRangedGrabInteractionModule grabbableToDrop))
                        {
                            grabbableToDrop.RequestLocalDrop(interactorID);
                        }
                    }

                    // Teleport grabbable to be at interactor to avoid anything in the way 
                    OnRequestTeleportRigidbody?.Invoke(interactor.GrabberTransform.position);

                    // Set grabbed in normal way
                    SetGrabbed(interactorID);

                    // Lock grab if lockGrab is true, but don't unlock grab if already grabbed
                    _grabIsLocked = lockGrab;

                    return true;

                }
            }

            return false;
        }

        public void ForceLocalGrab(bool lockGrab, VRHandInteractorType handToGrabWith)
        {
            bool forceDrop = true;
            DoLocalGrab(lockGrab, handToGrabWith, forceDrop);
        }

        public bool TryLocalGrab(bool lockGrab, VRHandInteractorType priorityHandToGrabWith = VRHandInteractorType.RightHandVR)
        {

            VRHandInteractorType[] handOrder = priorityHandToGrabWith == VRHandInteractorType.RightHandVR
                ? new[] { VRHandInteractorType.RightHandVR, VRHandInteractorType.LeftHandVR }
                : new[] { VRHandInteractorType.LeftHandVR, VRHandInteractorType.RightHandVR };

            foreach (var hand in handOrder)
            {
                if (DoLocalGrab(lockGrab, hand, false))
                {
                    return true;
                }
            }

            return false;

        }

        public void UnlockLocalGrab() => _grabIsLocked = false;

        public void ForceLocalDrop()
        {
            if (IsGrabbed)
            {
                UnlockLocalGrab();
                // Set dropped in normal way
                SetDropped(_state.MostRecentInteractingInteractorID);
            }
        }
    }

    [Serializable]
    internal class GrabbableState : VE2Serializable
    {
        public ushort StateChangeNumber { get; set; }
        public bool IsGrabbed { get; set; }
        public InteractorID MostRecentInteractingInteractorID { get; set; } //TODO: - maybe this should just be a string? Maybe InteractorID doesn't need to be a VE2Serializable

        public GrabbableState()
        {
            StateChangeNumber = 0;
            IsGrabbed = false;
            MostRecentInteractingInteractorID = new InteractorID(ushort.MaxValue, InteractorType.None);
        }

        public GrabbableState(byte[] bytes) : base(bytes) { }

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
