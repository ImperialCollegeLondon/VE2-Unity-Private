using System;
using UnityEngine;
using ViRSE.Core.Shared;

namespace ViRSE.PluginRuntime.VComponents
{
    [Serializable]
    public class PushActivatableConfig
    {
        [SerializeField, HideLabel] public ActivatableStateConfig StateConfig = new();
        [SpaceArea(spaceBefore: 10), SerializeField] public GeneralInteractionConfig GeneralInteractionConfig = new();
        [SpaceArea(spaceBefore: 10), SerializeField] public RangedInteractionConfig RangedInteractionConfig = new();
    }

    public class V_PushActivatable : MonoBehaviour, IPushActivatable, IRangedClickPlayerInteractableImplementor, ICollidePlayerInteratableImplementor
    {
        [SerializeField, HideLabel] private PushActivatableConfig _config = new();
        [SerializeField, HideInInspector] private SingleInteractorActivatableState _state = new();

        private PushActivatable _pushActivatable;

        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule ISingleInteractorActivatableStateModuleImplementor._module => _pushActivatable.StateModule;
        IGeneralInteractionModule IGeneralInteractionModuleImplementor._module => _pushActivatable.GeneralInteractionModule;
        IRangedInteractionModule IRangedInteractionModuleImplementor._module => _pushActivatable.RangedClickInteractionModule;
        #endregion

        #region Player Rig Interfaces
        IGeneralPlayerInteractable IGeneralPlayerInteractableImplementor.GeneralPlayerInteractable => _pushActivatable.GeneralInteractionModule;
        IRangedPlayerInteractable IRangedPlayerInteractableImplementor.RangedPlayerInteractable => _pushActivatable.RangedClickInteractionModule;
        IRangedClickPlayerInteractable IRangedClickPlayerInteractableImplementor.RangedClickPlayerInteractable => _pushActivatable.RangedClickInteractionModule;
        ICollidePlayerInteratable ICollidePlayerInteratableImplementor.CollidePlayerInteratable => _pushActivatable.ColliderInteractionModule;
        #endregion

        private void Start()
        {
            _pushActivatable = PushActivatableFactory.Create(_config, _state, gameObject.name);
        }
    }

    public static class PushActivatableFactory
    {
        public static PushActivatable Create(PushActivatableConfig config, ViRSESerializable state, string goName)
        {
            SingleInteractorActivatableStateModule stateModule = new(state, config.StateConfig, goName);
            GeneralInteractionModule GeneralInteractionModule = new(config.GeneralInteractionConfig); 
            RangedClickInteractionModule RangedClickInteractionModule = new(config.RangedInteractionConfig);
            ColliderInteractionModule ColliderInteractionModule = new();

            return new PushActivatable(stateModule, GeneralInteractionModule, RangedClickInteractionModule, ColliderInteractionModule);
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
        #endregion

        public PushActivatable(
            SingleInteractorActivatableStateModule stateModule,
            GeneralInteractionModule generalInteractionModule,
            RangedClickInteractionModule rangedClickInteractionModule,
            ColliderInteractionModule colliderInteractionModule)
        {
            StateModule = stateModule;
            GeneralInteractionModule = generalInteractionModule;
            RangedClickInteractionModule = rangedClickInteractionModule;
            ColliderInteractionModule = colliderInteractionModule;
            RangedClickInteractionModule.OnClickDown += HandleOnInteract;
            ColliderInteractionModule.OnCollideEnter += HandleOnInteract;
        }

        //TODO - all of these things are going to be the same across multiple VCs, 
        //Maybe we just inject the state module into the network and interaction modules directly? 

        private void HandleOnInteract(InteractorID interactorID)
        {
            //Could live in interactor, call InvertState on state module, which would emit an event to the network module "OnStateChanged"
            StateModule.InvertState(interactorID);
        }
    }
}


/*
 * If we want to be able to add our own syncer, this is going to have to go through the regular plugin interface 
 * Ok, so what is the sync module currently doing?
 * Needs to be told about state changes, so it can transmit 
 * That's fine, we can just have the sync module listen to the state module, and then transmit when it gets a state change
 * Except, it needs to know NOT to transmit when the state change froms from the host.
 * So the sync module needs its own interal Byte Array, wit holds its OWN state change flag
 * When it receives from the the non-host, it goes through the plugin api to change state
 * Then, it receives that new state, if the bytes don't match what it currently has, then it's a new state, and it should transmit 
 * When it receives a new state from the host, it updates its own byte array BEFORE changing state via the plugin API
 * When the VC then emits that new state, the sync module doesn't consider it to be a new state, since it matches its byte array
 * 
 * Ideally, we want this network stuff to appear in the VC's inspector... how do we achieve that? 
 * We could override the the entire VC, and add in the network stuff
 * This makes it tricky to migrate to a networked project though... all the components would basically be "V_PushActivatableSyncable"
 * Ideally then, what we want, is some slot for some generic "NetworkModule" that we can slot in
 * But this is PURELY for the inspector, and to actually create the network module component at runtime...
 * 
 * VC already exists, for each VC, we need to add some settings, networked yes/no, freq, protocol 
 * We then need a network module to be created at runtime, which needs to be linked to the VCs event for "OnNewStateBytes"
 * So for each VC, we need to somehow add new mono??
 * 
 * WHAT IF, we just override the BaseStateHolder class!!!
 * That works for predictive syncables... what about everything else?
 * We can do some type check, if its an RB, then we actually need an RB sync module
 * Now we're cooking...
 * 
 * 
 * If we do keep multiplayer proprietary... then what? Are people still allowed to host their own servers? 
 * If they want to write their own custom netcode, then ya sure 
 * Otherwise, no, they have to go through us 
 * How does THAT work? They need a local server anyway, so they can just run that on the cloud somewhere? 
 * Maybe it's only multiplyer with our netcode if it's on the platform. Our sealed code points things towards the local server for testing 
 * But in the build, it points towards whatever server its told to point to 
 * We WOULD want people to configure this editor IP, so they can test their own server 
 * Our private code just does a check to say "are we in build? If not, use the IP we receive from the platform"
 * 
 * This then means multiplayer apps MUST be on the platform, and they MUST use our netcode
 * We could potentially also release our own netcode alternatives 
 * 
 * So e.g Kings college, even if they have a standalone application, that will connect to the platform, the platform then points them towards 
 * their own private instance relay server 
 * 
 * Maybe some custom editor thing? 
 */