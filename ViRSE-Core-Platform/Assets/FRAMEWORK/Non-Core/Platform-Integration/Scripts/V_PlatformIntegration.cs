using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using ViRSE.Core.Shared;
using ViRSE.PluginRuntime;
using static PlatformSerializables;

namespace ViRSE.InstanceNetworking
{
    public class V_PlatformIntegration : MonoBehaviour, IPlatformService //will need some customer-facing interfaces
    {
        //If isPlatform, these settings will be overriden by whatever the platform says
        [SerializeField] private ServerType _serverType = ServerType.Local;
        [SerializeField] private string _localServerIP = "127.0.0.1";
        [SerializeField] private string _remoteServerIP = "";
        [SerializeField] private ushort portNumber = 4296;

        [SerializeField] private bool UseSpoofUserIdentity = false;
        [SerializeField] UserIdentity spoofUserIdentity;

        private PlatformService _platformService;
        private PlatformService PlatformService  //We'll add platform components to the scene on start (e.g global info UI), this will let those components access the platform after domain reload
        {
            get  {
                if (_platformService == null)
                    Awake();

                return _platformService;
            }
            set {
                _platformService = value;
            }
        }

        #region Instance-Sync-Facing Interfaces
        public bool IsConnectedToServer => _platformService.IsConnectedToServer;
        public event Action OnConnectedToServer { add => _platformService.OnConnectedToServer += value; remove => _platformService.OnConnectedToServer -= value; }
        public InstanceConnectionDetails GetInstanceConnectionDetails() => _platformService.GetInstanceConnectionDetails();
        //Connect to server 
        #endregion

        //Instance allocation request is going to have to be in a private interface
        //No, NONE of this platform stuff is going to be accessible to the customer. 
        //True, they'll have the actual interface definition, but none of the concretes 
        //Right, but that means they'll be able to then search for that interface at runtime, and call it 
        //So we need an interface that's literally just "ConnectionDetailsProvider" then?

        /*   REQS
         *   We want the customer to be able to change the IP address for the INSTANCE syncer, and trigger the instance syncer connection
         *        -If we have that API, why not just have the platform do it that way?
         *        -Because it's not the platform's job, the instance-sync-facing platform API is ONLY meant for retrieving the connection details, and that's it
         *        -If the customer gets those details... then... it's fine? 
         *   
         *   The instance syncer should use the CDs from the platform, if present, otherwise use the customers
         *   
         *   We don't want the customer changing the instance sync CDs if on platform, or if the connection has been made already
         *   
         *   
         *   So surely we're fine 
         *   
         *   
         *   What we DO need, is an interface for instance allocation, getting the global info, etc. But that interface can be marked internal, so other namespaces can't see it!
         *   
         *   
         *   Anything that talks to the platform, should do it through this MonoBehaviour. They'll all have to find a reference to this Mono through FindObject, but that's fine, not too many objects needing to do that
         * 
         * What should the platformService actually DO?
         * YEsterday we talked about it purely being a comms point for the platform
         * That's fine for the UI, instance allocations, getting InstSync CDs, etc
         * Still to figure 
         *   - Moving us between instances, including deciding if we go to the tutorial 
         *   - Getting the player settings? 
         *      I suppose the player itself should pull the settings from the platform?
         *      That doesn't work though, player is open source! 
         *      We floated the idea of having the platform implement some generic "IPConnectionDetailsProvider" interface, and then having the InstSync pulling details from that... which basically IS what we are doing 
         *      We could have the player function in a similar way, we could have some generic "PlayerSettingsProvider" interface, and then have the player pull settings from that (this interface would be implemented by the platform)
         *      The alternative would be to reverse things, and have the PlatformService be responsible for pushing the settings to the player... not sure if that should really be the platform's job, though? 
         *      Another question here is whether we're worried about the customer overriding these settings at runtime. Since the player is the thing to be in charge of this... customers could easily add their own PlayerSettingsProvider, right?
         *      Well, yeah, but then we could also just build something to check for that during paltform export 
         *      The platform could even use some modified version of the player, that disallows the customer from changing the settings
         *      Even if we flip it, and the platform pushes settings to the player, we'd have to work around the same problem, nothing to stop the customer from overriding this later unless we build something in to prevent it 
         *      
         * The options I see 
         *   1. The config for the player doesn't actually live on the player spawner mono at all. Instead, it lives on some "PlayerSettingsProvider" object.... the platform has its own version of this. 
         *   2. The config DOES live on the player spawner mono, but if there's a "SettingsProvider" in the scene, the config is disabled on the spawner. 
         *      Is that all that much different really? Either way, there's a settings provider object the player spawner is pulling from 
         *   3. The config lives on the player spawner, but this is then overriden by the platform at runtime.
         *   
         *   It might actually make a lot of sense to have some kind of player settings provider... this would let COF customers build their own settings systems 
         *      Ok, and how does that work with PlayerPrefs? 
         *      The player settings provider has a bool for "EnablePlayerSettingsUI", if that is true, we have a "SavePlayerSettingsInPlayerPrefs" bool, which will save the settings to player prefs
         *      The player itself can emit a "PlayerSettingsChanged" event... we'll need this for the platform anyway to know when to send those settings back to the server 
         *      
         *      The question now I think becomes "does the player spawner have its own settings, or do those settings ONLY exist on the settings provider?"
         *      Well, the default settings provider object that we provide could live on the player spawner anyway, so it's not like the customer would have to go looking... and there's some nice consistency to be found in ALWAYS pulling settings from the provider 
         *      The player will still need defaults though, if there isn't a settings provider in the scene
         *      We already have this pattern of VCs changing their inspectors based on what's on the scene, can still have the player spawner do this too (although it would be a matter of DISABLING the inspector if there's a provider object)
         *      
         *      What happens with the 2d/vr enabled config??? That can just be part of the control config stuff that gets passed to the player, that's fine I think 
         *      
         *  Right, potential approach 
         *     PlayerSpawner has its own control config. This includes 2d, vr, use settings menu, if settings menu true, show a "OnSettingsChanged" event, and a "SaveSettingsToPlayerPrefs" bool
         *   
         * 
         * 
         */


        private void Awake()
        {
            if (_platformService != null)
                return;

            Debug.Log("Platform integration mono awake!");

            if (_serverType != ServerType.Offline) //TODO, does offline even make sense here?
            {
                string ipAddressString;
                ipAddressString = _serverType == ServerType.Local ? _localServerIP : _remoteServerIP;

                if (IPAddress.TryParse(ipAddressString, out IPAddress ipAddress) == false)
                    throw new System.Exception("Invalid IP address, could not connect to server");

                _platformService = PlatformServiceFactory.Create(GetUserIdentity(), ipAddress, portNumber);
            }
        }

        private UserIdentity GetUserIdentity()
        {
            if (UseSpoofUserIdentity && Application.isEditor)
            {
                return spoofUserIdentity;
            }
            else
            {
                UserIdentity userIdentity = null;

                try
                {
                    string[] commandLine = System.Environment.GetCommandLineArgs();

                    if (commandLine.Length >= 7)
                    {
                        userIdentity = new(
                            commandLine[2],
                            commandLine[3],
                            commandLine[4],
                            commandLine[5],
                            commandLine[6]);
                    }
                }
                catch { }

                if (userIdentity == null)
                    userIdentity = new UserIdentity("test", "guest", "first", "last", "machine");

                return userIdentity;
            }
        }

        private void FixedUpdate()
        {
            //PlatformServuce.NetworkUpdate();
        }

        //TODO, API to change instance code, and to connect/disconnect
        //API for connect, disconnect, change instance code 

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                //Disable the player config settings on the spawner??
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {

            }
            else
            {
                _platformService.TearDown();
            }
        }
    }
}


