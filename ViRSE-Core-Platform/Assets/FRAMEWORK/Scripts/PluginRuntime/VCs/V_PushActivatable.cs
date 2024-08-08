using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace ViRSE.PluginRuntime.VComponents
{
    //TODO 
    //How does the state get into the network module?
    //Figure out customer interfaces 
    //Remove ID from state
    //Figure out MonoBehaviour

    //Need to be careful to separate customer interfaces from interactor interfaces

    [Serializable]
    public class PushActivatableConfig
    {
        [SerializeField, HideLabel] public ActivatableStateConfig StateConfig;
        [PropertySpace(SpaceBefore = 10), SerializeField, HideLabel] public GeneralInteractionConfig GeneralInteractionConfig;
        [PropertySpace(SpaceBefore = 10), SerializeField, HideLabel] public RangedInteractionConfig RangedInteractionConfig;
        [PropertySpace(SpaceBefore = 10), SerializeField, HideLabel] public WorldStateSyncableConfig NetworkConfig = new("PushButton");
    }

    public class V_PushActivatable : MonoBehaviour, IPushActivatable, IRangedClickPlayerInteractableImplementor, ICollidePlayerInteratableImplementor
    {
        private PushActivatable _pushActivatable;

        [SerializeField, HideLabel] private PushActivatableConfig _config;

        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule ISingleInteractorActivatableStateModuleImplementor._module => _pushActivatable.StateModule;
        IGeneralInteractionModule IGeneralInteractionModuleImplementor._module => _pushActivatable.GeneralInteractionModule;
        IRangedInteractionModule IRangedInteractionModuleImplementor._module => _pushActivatable.RangedClickInteractionModule;
        #endregion

        #region Player Rig Interfaces
        IRangedPlayerInteractable IRangedPlayerInteractableImplementor.RangedPlayerInteractable => _pushActivatable.RangedClickInteractionModule;
        IRangedClickPlayerInteractable IRangedClickPlayerInteractableImplementor.RangedClickPlayerInteractable => _pushActivatable.RangedClickInteractionModule;
        ICollidePlayerInteratable ICollidePlayerInteratableImplementor.CollidePlayerInteratable => _pushActivatable.ColliderInteractionModule;
        #endregion

        //Maybe we just want to serialize the state and hold it outside the module
        private void Awake() //TODO - will be called on domain reload, meaning we lose the state module (which we need, for its state!)
        {
            _pushActivatable = new PushActivatable(_config, gameObject.name);
        }
    }

    [Serializable]
    public class PushActivatable
    {
        #region Modules
        public SingleInteractorActivatableStateModule StateModule { get; private set; }
        public GeneralInteractionModule GeneralInteractionModule { get; private set; }
        public RangedClickInteractionModule RangedClickInteractionModule { get; private set; }
        public ColliderInteractionModule ColliderInteractionModule { get; private set; }
        public PredictiveWorldStateSyncableModule PredictiveSyncableModule { get; private set; }
        #endregion

        public PushActivatable(PushActivatableConfig config, string goName)
        {
            StateModule = new(config.StateConfig, goName);
            StateModule.OnProgrammaticStateChangeFromPlugin += HandleProgrammaticStateChangeFromPlugin;

            RangedClickInteractionModule = new(config.RangedInteractionConfig);
            RangedClickInteractionModule.OnClickDown += HandleOnInteract;

            ColliderInteractionModule = new();
            ColliderInteractionModule.OnCollideEnter += HandleOnInteract;

            //TODO - SyncModule shouldn't be able to modify state, we should pass it as an interface, rather than than a reference to the concrete class - syncer only needs to get bytes anyway
            //TODO, wait until we're redy to register before adding syncable component
            //If the config says networked is false, we can just not add the module at all?
            if (ViRSEManager.Instance.ServerType != ServerType.Offline)
            {
                PredictiveSyncableModule = new(config.NetworkConfig, StateModule.State, WorldStateSyncer.Instance, goName);
                PredictiveSyncableModule.OnReceivedStateWithNoHistoryMatch += HandleReceiveRemoteOverrideState;
            }
        }
        private void HandleOnInteract(InteractorID interactorID)
        {
            StateModule.InvertState(interactorID);
            PredictiveSyncableModule?.ForceTransmitNextCycle();
        }

        private void HandleProgrammaticStateChangeFromPlugin()
        {
            //When the plugin code has updated the state of the button, we need to sync it out!
            PredictiveSyncableModule?.ForceTransmitNextCycle();
        }

        //TODO, maybe don't need to transmit a bool if we're already transmitting an ID?
        private void HandleReceiveRemoteOverrideState(byte[] receivedStateBytes)
        {
            StateModule.UpdateToReceivedNetworkState(receivedStateBytes);

            //If we are the host, that means this override state came from the non-host 
            //THAT means we should broadcast this new state out to the other non-hosts next frame
            if (PluginSyncService.Instance.IsHost)
                PredictiveSyncableModule?.ForceTransmitNextCycle();
        }
    }
}
