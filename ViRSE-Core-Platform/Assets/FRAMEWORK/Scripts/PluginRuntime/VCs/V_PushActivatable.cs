using Sirenix.OdinInspector;
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
        IGeneralInteractionModule IGeneralInteractionModuleImplementor._module => _generalInteractionModule;
        IRangedInteractionModule IRangedInteractionModuleImplementor._module => _rangedClickInteractionModule;
        #endregion

        private void Awake()
        {
            _stateModule = gameObject.AddComponent<SingleInteractorActivatableStateModule>();
            _stateModule.hideFlags = HideFlags.HideInInspector;
            _stateModule.Initialize(_stateConfig);
            _stateModule.OnProgrammaticStateChangeFromPlugin += HandleProgrammaticStateChangeFromPlugin;

            _rangedClickInteractionModule = gameObject.AddComponent<RangedClickInteractionModule>();
            _rangedClickInteractionModule.hideFlags = HideFlags.HideInInspector;
            _rangedClickInteractionModule.Initialize(_rangedInteractionConfig);
            _rangedClickInteractionModule.OnClickDown.AddListener(HandleOnInteract);

            _colliderInteractionModule = gameObject.AddComponent<ColliderInteractionModule>();
            _colliderInteractionModule.hideFlags = HideFlags.HideInInspector;
            _colliderInteractionModule.OnCollideEnter.AddListener(HandleOnInteract);

            //TODO - SyncModule shouldn't be able to modify state, we should pass it as an interface, rather than than a reference to the concrete class - syncer only needs to get bytes anyway
            //TODO, wait until we're redy to register before adding syncable component
            //If the config says networked is false, we can just not add the module at all?

            if (ViRSEManager.Instance.ServerType != ServerType.Offline)
            {
                if (PluginSyncService.Instance.ReadyToSync)
                    CreateSyncModule();
                else
                    PluginSyncService.Instance.OnReadyToSync += CreateSyncModule;
            }
        }

        private void CreateSyncModule()
        {
            PluginSyncService.Instance.OnReadyToSync -= CreateSyncModule;

            _predictiveSyncableModule = gameObject.AddComponent<PredictiveWorldStateSyncableModule>();
            _predictiveSyncableModule.hideFlags = HideFlags.HideInInspector;
            _predictiveSyncableModule.Initialize(_networkConfig, _stateModule.State);
            _predictiveSyncableModule.OnReceivedStateWithNoHistoryMatch.AddListener(HandleReceiveRemoteOverrideState);
        }

        private void HandleOnInteract(InteractorID interactorID)
        {
            _stateModule.InvertState(interactorID);
            _predictiveSyncableModule?.ForceTransmitNextCycle();
        }

        private void HandleProgrammaticStateChangeFromPlugin()
        {
            //When the plugin code has updated the state of the button, we need to sync it out!
            _predictiveSyncableModule?.ForceTransmitNextCycle();
        }

        //TODO, maybe don't need to transmit a bool if we're already transmitting an ID?
        private void HandleReceiveRemoteOverrideState(byte[] receivedStateBytes)
        {
            _stateModule.UpdateToReceivedNetworkState(receivedStateBytes);

            //If we are the host, that means this override state came from the non-host 
            //THAT means we should broadcast this new state out to the other non-hosts next frame
            if (PluginSyncService.Instance.IsHost)
                _predictiveSyncableModule?.ForceTransmitNextCycle();
        }
    }
}
