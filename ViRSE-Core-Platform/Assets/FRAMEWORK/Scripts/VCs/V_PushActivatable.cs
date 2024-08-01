using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace VComponents
{
    //TODO 
    //How does the state get into the network module?
    //Figure out customer interfaces 
    //Remove ID from state
    //Figure out MonoBehaviour

    public class V_PushActivatable : MonoBehaviour, IRangedInteractableComponent
    {
        #region config
        [SerializeField, HideLabel] private ActivatableStateConfig stateConfig;
        [PropertySpace(SpaceBefore = 10), SerializeField, HideLabel] private GeneralInteractionModuleConfig generalInteractionConfig;
        [PropertySpace(SpaceBefore = 10), SerializeField, HideLabel] private RangedInteractionConfig rangedInteractionConfig;
        [PropertySpace(SpaceBefore = 10), SerializeField, HideLabel] private WorldStateSyncableConfig networkConfig = new("PushButton");
        #endregion

        #region Modules
        private PushActivatableStateModule stateModule; //TODO should be "ToggleActivatableStateModule"?  Maybe? But then the handheld holds work the same way... maybe SingleInteractActivatable
        private RangedClickInteractionModule rangedClickInteractionModule;
        private ColliderInteractionModule colliderInteractionModule;
        private WorldStateSyncablePredictiveWrapper predictiveSyncableWrapper;
        #endregion

        public IRangedInteractionModule InteractionModule => rangedClickInteractionModule;

        private void Awake()
        {
            stateModule = new(stateConfig);

            rangedClickInteractionModule = new(rangedInteractionConfig, gameObject);
            rangedClickInteractionModule.OnComponentAwake();

            colliderInteractionModule = new();

            //TODO - SyncModule shouldn't be able to modify state, we should pass it as an interface, rather than than a reference to the concrete class - syncer only needs to get bytes anyway
            //PredictiveSyncModule can also probably just inherit from SyncModule
            predictiveSyncableWrapper = new(networkConfig, stateModule.State, gameObject, "Activatable");
            predictiveSyncableWrapper.OnComponentAwake();
        }

        private void Start()
        {
            rangedClickInteractionModule.OnClickDown.AddListener(OnInteract); 
            colliderInteractionModule.OnCollideEnter.AddListener(OnInteract);
            predictiveSyncableWrapper.OnReceivedStateWithNoHistoryMatch.AddListener(OnReceiveRemoteOverrideState);
        }


        private void OnInteract(InteractorID interactorID)
        {
            stateModule.InvertState(interactorID);
        }

        //TODO, maybe don't need to transmit a bool if we're already transmitting an ID?
        private void OnReceiveRemoteOverrideState(BaseSyncableState receivedState)
        {
            stateModule.SetState((PushActivatableState)receivedState);

            //If not coming from host, raise state change flag 
        }
    }
}


public abstract class PredictiveSyncableState : BaseSyncableState
{
    public int stateChangeNumber;

    protected PredictiveSyncableState(string id, int stateChangeNumber) : base(id)
    {
        this.stateChangeNumber = stateChangeNumber;
    }


    //NOTE - this doesn't check the state change number...
    //If we receive a new state where the actual state value matches but not the number, we don't want to override
    //But we _do_ still want to override the state change number

    public abstract bool CompareState(PredictiveSyncableState otherState);

    public bool CompareStateChangeNumber(PredictiveSyncableState otherState)
    {
        return stateChangeNumber == otherState.stateChangeNumber;
    }
}


//TODO - why can't the syncers literally just deal with binary strings? Similar to how PluginSyncable is working?
//Then, we wouldn't even need to deal with different types at the serialize/deserialize layer
//And we wouldn't need our syncablestates to override all these CompareObject methods
//Remember, we're no longer checking RB state on the recieving side, so we don't neead these fuzzy checks 


//What is actually going to go into the superclass here 
//The docs and code snippet buttons? 

//Something to wire it in to the subservices? Need to consider if its worth not just absorbing them if we're coupling them 
//to the inheritence heirarchy anyway though...




//Cleaning up 
/*
 * Right, we can't use execute always 
 * But we still need to be able to remove the submodules within the editor 
 * Option 1: The submodules could have a reference to their parent, if they detect their parent is null, they destroy themselves (when??)
 * 
 * Option 2: The parent module sends a "keep alive" message to the submodule, if the submodule doesn't receive this, it destroys itself (also, when??)
 * 
 * The problem with the two above is that this also needs to work in play mode, ideally don't want to have multiple different systems for this 
 * 
 * Option 3: Maybe there's some custom inspector nonsense we can swing
 * 
 * 
 * Option 4: Maybe these submodules don't actually NEED to be MonoBehaviours??
 * I mean, we need the mono methods, but the parent can just pass down those invokations can't they, 
 * This then means the interactor doesn't do GetComponent on the interaction module, it instead does GetComponent on the interface
 * THIS means we need an interface function for returning a reference to the submodule 
 * Maybe that even makes more sense? It means that we enter at the root level and search down, we don't ever have to worry about finding our way across branches of the tree (e.g RangedInteractionModule to GrabbableInteractionConfig)
 * The tradeoff is we have to call Awake/Start/Update manually from the parent 
 * But the pro, is that we don't need all the teardown stuff we'd need otherwise 
 * mmm, I think we do, 
 * 
 * 
 * 
 */