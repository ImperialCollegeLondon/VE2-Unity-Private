using Sirenix.OdinInspector;
using System;
using UnityEngine;

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
        [SerializeField, HideLabel] public ActivatableStateConfig StateConfig = new();
        [PropertySpace(SpaceBefore = 10), SerializeField, HideLabel] public GeneralInteractionConfig GeneralInteractionConfig = new();
        [PropertySpace(SpaceBefore = 10), SerializeField, HideLabel] public RangedInteractionConfig RangedInteractionConfig = new();
        [PropertySpace(SpaceBefore = 10), SerializeField, HideLabel] public WorldStateSyncableConfig NetworkConfig = new("PushButton");
    }

    //We want to test VC through the Interaction API, but the interaction API lives on the Mono! 
    //This is why I feel like its easier to not even HAVE a VC! 
    //Now we need a layer of piping from the Mono to the VC, AND a layer of piping from the VC to the VC su


    //This mono has to exist, and it's the one the customer goes through, so it's the one we should test 
    /*Unless there's some way to just test the VC directly, but that's going to be tricky without adding MORE wiring to the VC, which is what we're trying to avoid!
     * Even if we did, there would still be untested wiring in the mono class
     * Instead, we could just test the mono class, and then test the VC in isolation??
     * Or, figure out a way to test the mono... but how? 
     * We can't constructor inject anything into the mono, so how does this work without a DI framework??
     */
    public class V_PushActivatable : MonoBehaviour, IPushActivatable, IRangedClickPlayerInteractableImplementor, ICollidePlayerInteratableImplementor
    {
        [SerializeField, HideLabel] private PushActivatableConfig _config;

        [SerializeField, HideInInspector] private bool _setup = false;
        [SerializeField, HideInInspector] private SingleInteractorActivatableState _state = new();

        private PushActivatable _pushActivatable;

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

        private void Start()
        {
            if (!_setup)
                _state = new(); //persist the state through domain reloads

            _pushActivatable = PushActivatableFactory.Create(_config, _state, WorldStateSyncer.Instance, PluginRuntime.Instance.ServerType, gameObject.name);
        }
    }

    public static class PushActivatableFactory
    {
        public static PushActivatable Create(PushActivatableConfig config, ViRSENetworkSerializable state, WorldStateSyncer syncer, ServerType serverType, string goName)
        {
            SingleInteractorActivatableStateModule stateModule = new(config.StateConfig, state, goName);
            GeneralInteractionModule GeneralInteractionModule = new(config.GeneralInteractionConfig); 
            RangedClickInteractionModule RangedClickInteractionModule = new(config.RangedInteractionConfig);
            ColliderInteractionModule ColliderInteractionModule = new();

            PredictiveWorldStateSyncableModule PredictiveSyncableModule = serverType != ServerType.Offline ? 
                new(config.NetworkConfig, state, syncer, goName) : 
                null;

            return new PushActivatable(stateModule, GeneralInteractionModule, RangedClickInteractionModule, ColliderInteractionModule, PredictiveSyncableModule);
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

        //On domain reload, we're going to lose all the listeners here
        //How do we want to preserve them?
        //Maybe its easier to just have the entire VC set itself back up from scratch? 
        //All we need to preserve is the state module
        //Not even, we only need to preserve the actual state
        public PushActivatable(SingleInteractorActivatableStateModule stateModule,
        GeneralInteractionModule generalInteractionModule,
        RangedClickInteractionModule rangedClickInteractionModule,
        ColliderInteractionModule colliderInteractionModule,
        PredictiveWorldStateSyncableModule predictiveSyncableModule)
        {
            StateModule = stateModule;
            GeneralInteractionModule = generalInteractionModule;
            RangedClickInteractionModule = rangedClickInteractionModule;
            ColliderInteractionModule = colliderInteractionModule;
            PredictiveSyncableModule = predictiveSyncableModule;

            StateModule.OnProgrammaticStateChangeFromPlugin += HandleProgrammaticStateChangeFromPlugin;
            RangedClickInteractionModule.OnClickDown += HandleOnInteract;
            ColliderInteractionModule.OnCollideEnter += HandleOnInteract;

            //TODO - SyncModule shouldn't be able to modify state, we should pass it as an interface, rather than than a reference to the concrete class - syncer only needs to get bytes anyway
            //TODO, wait until we're redy to register before adding syncable component
            //If the config says networked is false, we can just not add the module at all?
            if (PredictiveSyncableModule != null)
                PredictiveSyncableModule.OnReceivedStateWithNoHistoryMatch += HandleReceiveRemoteOverrideState;
        }

        //TODO - all of these things are going to be the same across multiple VCs, 
        //Maybe we just inject the state module into the network and interaction modules directly? 

        private void HandleOnInteract(InteractorID interactorID)
        {
            //Could live in interactor, call InvertState on state module, which would emit an event to the network module "OnStateChanged"
            StateModule.InvertState(interactorID);
            PredictiveSyncableModule?.ForceTransmitNextCycle();
        }

        private void HandleProgrammaticStateChangeFromPlugin()
        {
            //Could live in network module, listens to event from state module
            //When the plugin code has updated the state of the button, we need to sync it out!
            PredictiveSyncableModule?.ForceTransmitNextCycle();
        }

        //TODO, maybe don't need to transmit a bool if we're already transmitting an ID?
        private void HandleReceiveRemoteOverrideState(byte[] receivedStateBytes)
        {
            //TODO - could live in network module, listening to an event StateModule
            StateModule.UpdateToReceivedNetworkState(receivedStateBytes);

            //If we are the host, that means this override state came from the non-host 
            //THAT means we should broadcast this new state out to the other non-hosts next frame
            if (PluginSyncService.Instance.IsHost)
                PredictiveSyncableModule?.ForceTransmitNextCycle();
        }
    }
}
