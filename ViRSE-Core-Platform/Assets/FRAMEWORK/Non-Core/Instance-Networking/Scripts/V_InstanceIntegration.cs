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

        #region ConnectionDebug
        [SerializeField, Disable] private ConnectionState _connectionState = ConnectionState.NotYetConnected;
        private enum ConnectionState { NotYetConnected, FetchingConnectionSettings, Connecting, Connected, LostConnection }
        #endregion

        [SerializeField, HideInInspector] private LocalClientIdWrapper LocalClientIDWrapper = new();
        [Serializable] public class LocalClientIdWrapper { public ushort LocalClientID = ushort.MaxValue; }

        private PluginSyncService _instanceService;
        public PluginSyncService InstanceService {
            get {
                if (_instanceService == null)
                    OnEnable();

                return _instanceService;
            }
            set {
                _instanceService = (PluginSyncService)value;
            }
        }

        // private bool _instanceNetworkSettingsProviderPresent => _instanceNetworkSettingsProvider != null;
        // private IInstanceNetworkSettingsProvider _instanceNetworkSettingsProvider => ViRSENonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider;


        #region Core-Facing Interfaces 
        public void RegisterStateModule(IStateModule stateModule, string stateType, string goName)
        {
            InstanceService.RegisterStateModule(stateModule, stateType, goName);
        }

        public void RegisterLocalPlayer(ILocalPlayerRig localPlayerRig)
        {
            InstanceService.RegisterLocalPlayer(localPlayerRig);
        }

        public void DeregisterLocalPlayer()
        {
            InstanceService.DeregisterLocalPlayer();
        }

        public bool IsEnabled => enabled && gameObject.activeInHierarchy;
        public string MultiplayerSupportGameObjectName => gameObject.name;
        #endregion


        //#region Platform-Facing Interfaces
        //public void SetInstanceNetworkSettings(InstanceNetworkSettings instanceNetworkSettings)         //TODO - need a customer facing version of this too
        //{
        //    _connectionDetails = instanceNetworkSettings;

        //    if (_instanceService != null)
        //    {
        //        ConnectToServer(); //ConnectToServerOnceDetailsReady
        //    }
        //}
        // #endregion

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
                _instanceService = PluginSyncServiceFactory.Create(LocalClientIDWrapper);
                _instanceService.OnConnectedToServer += () => _connectionState = ConnectionState.Connected;
                _instanceService.OnDisconnectedFromServer += () => _connectionState = ConnectionState.LostConnection;
            }

            if ((_connectionState == ConnectionState.NotYetConnected && _connectOnStart) || _connectionState == ConnectionState.LostConnection)
                ConnectToServerOnceDetailsReady();
        }

        private void ConnectToServerOnceDetailsReady() 
        {
            _connectionState = ConnectionState.FetchingConnectionSettings;

            if (ViRSENonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider.AreInstanceNetworkingSettingsReady)
                HandleNetworkSettingsProviderReady();
            else 
                ViRSENonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider.OnInstanceNetworkSettingsReady += HandleNetworkSettingsProviderReady;

            if (_connectionState != ConnectionState.FetchingConnectionSettings)
                return;

            if (ViRSECoreServiceLocator.Instance.PlayerSettingsProvider == null || ViRSECoreServiceLocator.Instance.PlayerAppearanceOverridesProvider == null || ViRSECoreServiceLocator.Instance.PlayerSettingsProvider.ArePlayerSettingsReady)
                HandlePlayerSettingsReady();
            else 
                ViRSECoreServiceLocator.Instance.PlayerSettingsProvider.OnPlayerSettingsReady += HandlePlayerSettingsReady;
        }

        private void HandleNetworkSettingsProviderReady()
        {
            ViRSENonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider.OnInstanceNetworkSettingsReady -= HandleNetworkSettingsProviderReady;

            if (ViRSECoreServiceLocator.Instance.PlayerSettingsProvider == null || ViRSECoreServiceLocator.Instance.PlayerAppearanceOverridesProvider == null || ViRSECoreServiceLocator.Instance.PlayerSettingsProvider.ArePlayerSettingsReady)
                HandleAllSettingsReady();
        }

        private void HandlePlayerSettingsReady() 
        {
            ViRSECoreServiceLocator.Instance.PlayerSettingsProvider.OnPlayerSettingsReady -= HandlePlayerSettingsReady;
            
            if (ViRSENonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider.AreInstanceNetworkingSettingsReady)
                HandleAllSettingsReady();
        }

        private void HandleAllSettingsReady() 
        {
            ViRSENonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider.OnInstanceNetworkSettingsReady -= HandleNetworkSettingsProviderReady;
            ViRSECoreServiceLocator.Instance.PlayerSettingsProvider.OnPlayerSettingsReady -= HandlePlayerSettingsReady;

            Debug.Log($"All settings ready, player spawner null? {ViRSECoreServiceLocator.Instance.PlayerSpawner == null} player settings null? {ViRSECoreServiceLocator.Instance.PlayerSettingsProvider == null} overrides null? {ViRSECoreServiceLocator.Instance.PlayerAppearanceOverridesProvider == null}");

            _connectionState = ConnectionState.Connecting;
            _instanceService.ConnectToServer( //TODO, DI through constructor? Maybe move all the waiting there too...
                ViRSENonCoreServiceLocator.Instance.InstanceNetworkSettingsProvider, 
                ViRSECoreServiceLocator.Instance.PlayerSpawner,
                ViRSECoreServiceLocator.Instance.PlayerSettingsProvider, 
                ViRSECoreServiceLocator.Instance.PlayerAppearanceOverridesProvider);
        }

        private void FixedUpdate()
        {
            _instanceService.NetworkUpdate(); //TODO, think about this... perhaps the syncers should be in charge of calling themselves?
        }


        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                //ViRSECoreServiceLocator.Instance.MultiplayerSupport = null;
                return;
            }

            _instanceService.DisconnectFromServer();
            //_instanceService.TearDown();
            // _instanceService = null;
        }

        private void OnDestroy() 
        {
            if (!Application.isPlaying)
            {
                return;
            }

            _instanceService.TearDown();
            _instanceService.OnConnectedToServer -= () => _connectionState = ConnectionState.Connected;
            _instanceService.OnDisconnectedFromServer -= () => _connectionState = ConnectionState.NotYetConnected;
            _instanceService = null;

            _connectionState = ConnectionState.NotYetConnected;
        }
    }
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