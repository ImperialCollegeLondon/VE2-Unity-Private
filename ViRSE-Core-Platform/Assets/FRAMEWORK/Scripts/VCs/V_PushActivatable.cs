using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace VComponents
{
    //TODO - rename to V_PushButton
    [ExecuteAlways]
    public class V_NewActivatable : MonoBehaviour
    {
        //[FoldoutGroup("Activatable Settings V Group/Activatable Settings")]
        //[PropertyOrder(-15)]
        [SerializeField][ShowInInspector] public UnityEvent OnActivate;

        //[FoldoutGroup("Activatable Settings V Group/Activatable Settings")]
        //[PropertyOrder(-15)]
        [SerializeField] [ShowInInspector] public UnityEvent OnDeactivate;

        [InlineEditor(InlineEditorObjectFieldModes.CompletelyHidden)]
        public RangedClickInteractionModule rangedClickInteractionModule;

        [InlineEditor(InlineEditorObjectFieldModes.CompletelyHidden)]
        public ColliderInteractionModule colliderInteractionModule;

        [InlineEditor(InlineEditorObjectFieldModes.CompletelyHidden)]
        public WorldStateSyncablePredictiveWrapper predictiveSyncableWrapper;

        public PushActivatableState state;

        //Hides components in inspector just fine
        [OnInspectorInit]
        private void CreateData()
        {
            if (rangedClickInteractionModule == null)
            {
                rangedClickInteractionModule = gameObject.AddComponent<RangedClickInteractionModule>();
                rangedClickInteractionModule.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
            }

            if (colliderInteractionModule == null)
            {
                colliderInteractionModule = gameObject.AddComponent<ColliderInteractionModule>();
                colliderInteractionModule.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
            }

            if (predictiveSyncableWrapper == null)
            {
                predictiveSyncableWrapper = gameObject.AddComponent<WorldStateSyncablePredictiveWrapper>();
                predictiveSyncableWrapper.hideFlags = HideFlags.HideInInspector | HideFlags.NotEditable;
                predictiveSyncableWrapper.InitializeInspector("Activatable");
            }
        }

        private void InitializeRuntime()
        {
            //TODO, maybe we don't want to worry about IDs at this level?
            //TODO, we need to wire this starting state into the the actual network module?
            //Maybe it's jank that the VC constantly has to actually send its state to the NM?
            //Unless state in the NM always starts as null? If it's null, just don't transmit
            //state = new(predictiveSyncableWrapper.Id, 0, false, -1);

            rangedClickInteractionModule.OnClickDown.AddListener(OnInteract);
            colliderInteractionModule.OnCollideEnter.AddListener(OnInteract);

            predictiveSyncableWrapper.ReceivedStateWithNoHistoryMatch.AddListener(OnReceiveRemoteOverrideState);
        }

        //private void FixedUpdate()
        //{
        //    predictiveSyncableWrapper.state
        //}

        //This could be fired off from either the collision interaction module or from the click interaction module
        //We probably want to route both of those into the same method? 

        //Do we want activatables being told each frame what's colliding with them, or instead just be on click down and on click release? 
        //Well, for toggle, we want something to fire off every time 
        //Right, but that could also just be some generic "onInteract" thing, and the IM only fires it off if its hold?
        //For hold, we want "OnDown" and "OnUp" so we know to add or remove people 
        //In that case, do we really need "stay?" I don't think so!

        //Ok, without 'stay', how does the host deal with the fact that click releases may have been lost?
        //Well, maybe that always has to go over TCP?
        //But, when they drop connection, the host will still think they're holding down
        //The activatable would have to listen to the connection drop? 
        //OR, it _is_ just something that happens every frame 
        //Remote interactors could transmit what they're currently holding down?
        //

        private void OnInteract(InteractorID interactorID)
        {
            state.isActivated = !state.isActivated;

            if (state.isActivated)
                InvokeCustomerOnActivateEvent();
            else
                InvokeCustomerOnDeactivateEvent();

            state.activatingUserID = state.isActivated? interactorID.ClientID : -1;

            //Regardless of whether we're host, we want to transmit this state
            //predictiveSyncableWrapper.UpdateAndTransmitState(state);
        }

        //TODO, maybe don't need to transmit a bool if we're already transmitting an ID?
        private void OnReceiveRemoteOverrideState(BaseSyncableState otherBaseSyncable)
        {
            PushActivatableState other = otherBaseSyncable as PushActivatableState;

            bool activationStateHasChanged = false;

            if (!state.isActivated && other.isActivated) //Activating local
            {
                activationStateHasChanged = true;
                state.activatingUserID = other.activatingUserID;
                state.isActivated = true;
                InvokeCustomerOnActivateEvent();
            }
            else if (state.isActivated && !other.isActivated) //Deactivating local
            {
                activationStateHasChanged = true;
                state.activatingUserID = -1;
                state.isActivated = false;
                InvokeCustomerOnDeactivateEvent();
            }

            //The host should be able to override the non-hosts 
            //if (StaticData.Networking.IsHost() && activationStateHasChanged)
            //{
            //    state.stateChangeNumber++;
            //    predictiveSyncableWrapper.UpdateAndTransmitState(state);
            //}
            //else if (!StaticData.Networking.IsHost())
            //{
            //    state.stateChangeNumber = other.stateChangeNumber;
            //    state.activatingUserID = other.activatingUserID;
            //}
        }

        private void InvokeCustomerOnActivateEvent()
        {
            OnActivate?.Invoke();
        }

        private void InvokeCustomerOnDeactivateEvent()
        {
            OnDeactivate?.Invoke();
        }

        private void OnDestroy()
        {
            if (rangedClickInteractionModule != null)
                rangedClickInteractionModule.TearDown();

            if (colliderInteractionModule != null)
                colliderInteractionModule.TearDown();

            if (predictiveSyncableWrapper != null)
                predictiveSyncableWrapper.TearDown();
        }
    }
}

public class PushActivatableState : NonPhysicsSyncableState
{
    public bool isActivated;
    public int activatingUserID;

    public PushActivatableState(string id, int stateChangeNumber, bool isActivated, int activatingUserID) : base(id, stateChangeNumber)
    {
        this.isActivated = isActivated;
        this.activatingUserID = activatingUserID;
    }

    public override bool CompareState(NonPhysicsSyncableState otherState)
    {
        PushActivatableState other = (PushActivatableState)otherState;

        return (isActivated == other.isActivated && activatingUserID == other.activatingUserID);
    }
}

public abstract class NonPhysicsSyncableState : BaseSyncableState
{
    public int stateChangeNumber;

    protected NonPhysicsSyncableState(string id, int stateChangeNumber) : base(id)
    {
        this.stateChangeNumber = stateChangeNumber;
    }


    //NOTE - this doesn't check the state change number...
    //If we receive a new state where the actual state value matches but not the number, we don't want to override
    //But we _do_ still want to override the state change number

    public abstract bool CompareState(NonPhysicsSyncableState otherState);

    public bool CompareStateChangeNumber(NonPhysicsSyncableState otherState)
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
