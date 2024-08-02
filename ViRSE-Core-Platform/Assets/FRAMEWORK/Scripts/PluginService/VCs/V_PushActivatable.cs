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

    //Need to be careful to separate customer interfaces from interactor interfaces

    public class V_PushActivatable : MonoBehaviour, IPushActivatable
    {
        #region config
        [SerializeField, HideLabel] private ActivatableStateConfig _stateConfig;
        [PropertySpace(SpaceBefore = 10), SerializeField, HideLabel] private GeneralInteractionConfig _generalInteractionConfig;
        [PropertySpace(SpaceBefore = 10), SerializeField, HideLabel] private RangedInteractionConfig _rangedInteractionConfig;
        [PropertySpace(SpaceBefore = 10), SerializeField, HideLabel] private WorldStateSyncableConfig _networkConfig = new("PushButton");
        #endregion

        #region Modules
        private SingleInteractorActivatableStateModule _stateModule;
        private GeneralInteractionModule _generalInteractionModule;
        private RangedClickInteractionModule _rangedClickInteractionModule;
        private ColliderInteractionModule _colliderInteractionModule;
        private PredictiveWorldStateSyncableModule _predictiveSyncableModule;
        #endregion

        #region PluginInterfaces
        ISingleInteractorActivatableStateModule ISingleInteractorActivatableStateModuleImplementor._module => _stateModule;
        IGeneralInteractionModule IGeneralInteractionModuleImplementor.module => _generalInteractionModule;
        IRangedInteractionModule IRangedInteractionModuleImplementor.module => _rangedClickInteractionModule;
        #endregion

        private void Awake()
        {
            _stateModule = new(_stateConfig);

            _rangedClickInteractionModule = gameObject.AddComponent<RangedClickInteractionModule>();
            _rangedClickInteractionModule.Initialize(_rangedInteractionConfig);
            _rangedClickInteractionModule.OnClickDown.AddListener(HandleOnInteract);

            _colliderInteractionModule = gameObject.AddComponent<ColliderInteractionModule>();
            _colliderInteractionModule.OnCollideEnter.AddListener(HandleOnInteract);

            //TODO - SyncModule shouldn't be able to modify state, we should pass it as an interface, rather than than a reference to the concrete class - syncer only needs to get bytes anyway
            //TODO, wait until we're redy to register before adding syncable component
            _predictiveSyncableModule = gameObject.AddComponent<PredictiveWorldStateSyncableModule>();
            _predictiveSyncableModule.Initialize(_networkConfig, _stateModule.State);
            _predictiveSyncableModule.OnReceivedStateWithNoHistoryMatch.AddListener(OnReceiveRemoteOverrideState);
        }

        private void HandleOnInteract(InteractorID interactorID)
        {
            _stateModule.InvertState(interactorID);
            _predictiveSyncableModule.ForceTransmitNextCycle();
        }

        //TODO, maybe don't need to transmit a bool if we're already transmitting an ID?
        private void OnReceiveRemoteOverrideState(BaseSyncableState receivedState)
        {
            _stateModule.SetState((SingleInteractorActivatableState)receivedState);

            if (InstanceSyncService.IsHost)
                _predictiveSyncableModule.ForceTransmitNextCycle();
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

    public abstract bool CompareState(PredictiveSyncableState otherState);

    public bool CompareStateChangeNumber(PredictiveSyncableState otherState)
    {
        return stateChangeNumber == otherState.stateChangeNumber;
    }
}
