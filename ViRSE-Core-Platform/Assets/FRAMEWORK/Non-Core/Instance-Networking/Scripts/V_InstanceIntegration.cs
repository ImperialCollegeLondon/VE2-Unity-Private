using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Codice.Client.BaseCommands;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using ViRSE.Core.Player;
using ViRSE.Core.Shared;
using ViRSE.Networking;
using ViRSE.PluginRuntime;
using ViRSE.PluginRuntime.VComponents;
using static InstanceSyncSerializables;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.InstanceNetworking
{
    [ExecuteInEditMode]
    public class V_InstanceIntegration : MonoBehaviour, IMultiplayerSupport //, IInstanceNetworkSettingsReceiver
    {
        #region Inspector Fields
        // [DynamicHelp(nameof(_settingsMessage))]
        [SerializeField] private bool _connectOnStart = true;

        // private string _settingsMessage => _instanceNetworkSettingsProviderPresent ?
        //     $"Debug network settings can be found on the {_instanceNetworkSettingsProvider.GameObjectName} gameobject" :
        //     "If not connecting automatically, details should be passed via the API";
        #endregion

        [SerializeField, Disable, HideLabel, IgnoreParent] private ConnectionStateDebugWrapper _connectionStateDebug;

        [SerializeField, HideInInspector] private LocalClientIdWrapper LocalClientIDWrapper = new();
        [Serializable] public class LocalClientIdWrapper { public ushort LocalClientID = ushort.MaxValue; }

        private InstanceService _instanceService;
        private WorldStateSyncer _worldStateSyncer;
        private PlayerSyncer _playerSyncer;

        // private bool _instanceNetworkSettingsProviderPresent => _instanceNetworkSettingsProvider != null;
        // private IInstanceNetworkSettingsProvider _instanceNetworkSettingsProvider => ViRSENonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider;


        #region Core-Facing Interfaces
        //TODO - Could we follow the pattern set out by the VCs? Can we just stick this wiring in an interface?
        public void RegisterStateModule(IStateModule stateModule, string stateType, string goName)
        {
            if (_worldStateSyncer == null)
                OnEnable();

            _worldStateSyncer.RegisterStateModule(stateModule, stateType, goName);
        }
        
        public void RegisterLocalPlayer(ILocalPlayerRig localPlayerRig)
        {
            if (_playerSyncer == null)
                OnEnable();

            _playerSyncer.RegisterLocalPlayer(localPlayerRig);
        }
        
        public void DeregisterLocalPlayer() 
        {
            if (_playerSyncer == null)
                OnEnable();
            
            _playerSyncer.DeregisterLocalPlayer();
        }

        public bool IsEnabled => enabled && gameObject.activeInHierarchy;
        public string GameObjectName => gameObject.name;
        #endregion

        #region Debug Interfaces 
        //TODO, think about exactly we want to serve to the customer... OnRemotePlayerConnectd? GetRemotePlayers?
        public bool IsConnectedToServer => _connectionStateDebug.ConnectionState == ConnectionState.Connected;
        public ushort LocalClientID => LocalClientIDWrapper.LocalClientID;
        public InstancedInstanceInfo InstanceInfo => _instanceService.InstanceInfo; //TODO, don't want to expose this
        public event Action<InstancedInstanceInfo> OnInstanceInfoChanged;
        public event Action OnDisconnectedFromServer;
        #endregion

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                ViRSECoreServiceLocator.Instance.MultiplayerSupport = this;  
                return;
            }

            //But if we expose this to the customer... how does it go into the service locator???
            //Ok, we cam just make some base class for the settings provider that we can expose to the plugin, the plugin-defined settings provider can just inherit from that. The base can worry about putting itself into the service locator 
            if (ViRSENonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider == null)
            {
                Debug.LogError("Can't boot instance integration, no network settings provider found");
                return;
            }

            if (_instanceService == null)
            {
                _instanceService = InstanceServiceFactory.Create(LocalClientIDWrapper, _connectOnStart, _connectionStateDebug);
                    _instanceService.OnConnectedToServer += HandleConnectToServer; //TODO, maybe these events can go into the connection debug wrapper thing?
                    _instanceService.OnInstanceInfoChanged += HandleReceiveInstanceInfo;
            }

            // _instanceService ??= InstanceServiceFactory.Create(LocalClientIDWrapper, _connectOnStart, _connectionStateDebug); 
            _worldStateSyncer ??= new WorldStateSyncer(_instanceService);
            _playerSyncer ??= PlayerSyncerFactory.Create(_instanceService);

            // if ((_connectionStateDebug.ConnectionState == ConnectionState.NotYetConnected && _connectOnStart) || _connectionStateDebug.ConnectionState == ConnectionState.LostConnection)
            // {
            //     _instanceService.OnConnectedToServer += HandleConnectToServer; //TODO, maybe these events can go into the connection debug wrapper thing?
            //     _instanceService.OnInstanceInfoChanged += HandleReceiveInstanceInfo;
            // }
        }

        private void FixedUpdate()
        {
            _instanceService.NetworkUpdate(); 

            //TODO - maybe the service should emit an update event that the others listen to?
            _worldStateSyncer.NetworkUpdate();
            _playerSyncer.NetworkUpdate();
        }

        private void HandleConnectToServer() 
        {
            _instanceService.OnConnectedToServer -= HandleConnectToServer;
            _instanceService.OnDisconnectedFromServer += HandleDisconnectFromServer; //TODO, maybe these events can go into the connection debug wrapper thing?

        }

        private void HandleReceiveInstanceInfo(InstancedInstanceInfo instanceInfo) 
        {
            OnInstanceInfoChanged?.Invoke(instanceInfo);
        }

        private void HandleDisconnectFromServer()
        {
            _instanceService.OnDisconnectedFromServer -= HandleDisconnectFromServer;
            OnDisconnectedFromServer?.Invoke();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            _instanceService.DisconnectFromServer();
        }

        private void OnDestroy() 
        {
            if (!Application.isPlaying)
                return;

            _instanceService.TearDown();
            _instanceService = null;

            _connectionStateDebug.ConnectionState = ConnectionState.NotYetConnected;
        }
    }

    [Serializable]
    public class ConnectionStateDebugWrapper
    {
        [SerializeField, Disable, IgnoreParent] public ConnectionState ConnectionState = ConnectionState.NotYetConnected;
    }

    public enum ConnectionState { NotYetConnected, FetchingConnectionSettings, Connecting, Connected, LostConnection }

}



/*
    NOW the problem is the bloody avatar overrides 
    how does THIS WORK

    Maybe it DOES make sense for the player settings to come from the player spawner. It's the only place that has the overrides 
    The player spawner can just have its own proxy events for when the settings are ready, the instancer can listen to these 

    Ok, so we're now saying that "InstancedAvatarAppearance" lives in core
    That makes sense, it's IS used in the core part of the framework 
    Right, so we have "SaveableUserSettings", which have the saved bits of appearance 

    And then we also have the overrides, which AREN'T in the saved settings, but ARE combined with PART of the saved settings to generate the overall avatar appearance 
    We can't have the overrides be in the saved settings, because they're not saved!

    Does it make sense for these to not be grouped? 
    appearance comes from the platform, overrides don't, because they're specific to the actual applications 
    So they really do have to come from different places 

    ==================================================
    Currently, we have this concept of "InstancedAvatarAppearance", which is a combination of the saved settings and the overrides
    That then sits inside "InstancedClientInfo", which is the regular client info, with the overrides bolted on 
    The thing is, if we want to support off-platform multiplayer, the regular appearance (the one from client info) can't come from the platform 

    So when we're on platform, instanced, but we don't have a player spawner... then what the hell happens???
    We then have to account for that in the entire registration flow 
    This is why I wonder if we should rethink the ClientInfo system we have while instanced 
    Why can't this all just be part of the PlayerSyncer??

    We'd need to register with the server, but we don't pass any appearance info
    Thing is, if we want to be transmitting appearance info, tied to player IDs... should that NOT be in the client info?
    Instead, what if we just have the player syncer itself transmit out its appearance?
    It could do this on a lower frequency than the player position 
    that way we only have to worry about appearances when we actually have a player
    Bonus: The service itself isn't concerned with appearances... all it does it manage its own subsyncers 

    So how would this look?
    Player spawns (if it exists at all), it only spawns once its settings are ready 
    The player registers itself with the syncer, passing its appearance details 
    The player syncer then transmits its appearance details infrequently, and its position frequently 
    On the receiving side, the player syncer doesn't care about the ClientInfo, insteadm it just spawns and despawns players as it receives appearance details, and/or position updates
    DOESN'T WORK 
    What do we do when we want to despawn someone? How do we distinguish between a player that's changed to a different avatar, and a player that's left the game?
    We need some source of truth for who in the instance is actually HAS an avatar 
    The InstancedClientInfo DOES seem like a reasonable place for this.



    If we didn't have an appearance in the instance service... what do we show on the instance UI???
    Maybe we DON'T have an instance UI? Unless the customer wants to create their own, of course 
    ==================================================

    Right, so - back to AvatarAppearance coming directly from the PlayerSpawner 
    Or we could just have separate functions for AvatarSetting and AvatarOverrides 

    Ok, so, the instacer looks for a player spawner, if it finds one, it grabs its settings
    ANd then we're back to the problem... the instancer has to worry about the case of their not BEING a player spawner 
    In that case, it needs to pass a null appearance to the service, which in turn passes an null appearance to the server 



    ==================================================

    Would this really not be simpler if it was tied into the player syncer in some way??
    That does seem to make sense, right?
    We only have a player syncer if we 


    What happens when we want different player rigs within the same plugin??
    Urgh, let's not worry about that now I guess 

*/