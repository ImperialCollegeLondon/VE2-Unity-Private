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
    public class PlatformServiceProvider : MonoBehaviour, IPlatformServiceProvider
    {
        //If isPlatform, these settings will be overriden by whatever the platform says
        [SerializeField] private ServerType _serverType = ServerType.Local;
        [SerializeField] private string _localServerIP = "127.0.0.1";
        [SerializeField] private string _remoteServerIP = "";
        [SerializeField] private ushort _portNumber = 4296;

        [Space(10)]
        [SerializeField] private string startingInstanceSuffix = "dev";
        [SerializeField, HideInInspector] private string _instanceCode = null;

        [Space(10)]
        [SerializeField] private bool UseSpoofUserIdentity = false;
        [SerializeField] UserIdentity spoofUserIdentity;

        private IPlatformService _platformService;
        public IPlatformService PlatformService  //We'll add platform components to the scene on start (e.g global info UI), this will let those components access the platform after domain reload
        {
            get {
                if (_platformService == null)
                    OnEnable();

                return _platformService;
            }
        }

        private void OnEnable()
        {
            if (_platformService != null || !Application.isPlaying)
                return;

            DontDestroyOnLoad(gameObject);

            //Debug.Log("Platform integration mono awake!");

            if (string.IsNullOrEmpty(_instanceCode)) //Otherwise, the instance code will be carried over from its serialized state pre domain reload
                _instanceCode = PlatformInstanceInfo.GetInstanceCode(SceneManager.GetActiveScene().name, startingInstanceSuffix);

            if (_serverType != ServerType.Offline) //TODO, does offline even make sense here?
            {
                string ipAddressString;
                ipAddressString = _serverType == ServerType.Local ? _localServerIP : _remoteServerIP;

                if (IPAddress.TryParse(ipAddressString, out IPAddress ipAddress) == false)
                    throw new System.Exception("Invalid IP address, could not connect to server");

                _platformService = PlatformServiceFactory.Create(GetUserIdentity(), ipAddress, _portNumber, _instanceCode);
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

                if (!Application.isEditor)
                {
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
                }

                if (userIdentity == null)
                    userIdentity = new UserIdentity("test", "guest", "first", "last", "machine");

                Debug.Log($"Generated User identity: {userIdentity.ToString()}");

                return userIdentity;
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
                _platformService?.TearDown();
        }

        //#if UNITY_EDITOR
        //        [UnityEditor.InitializeOnLoadMethod]
        //        private static void RegisterDomainReloadCallback()
        //        {
        //            UnityEditor.AssemblyReloadEvents.afterAssemblyReload += HandleReload;
        //        }

        //        private static void HandleReload()
        //        {
        //            if (!Application.isPlaying)
        //                return;

        //            PlatformServiceProvider platformServiceProvider = FindObjectOfType<PlatformServiceProvider>();
        //            Debug.Log("Platform provider found? " + (platformServiceProvider != null));
        //            platformServiceProvider?.Awake();
        //        }
        //#endif
        //    }
    }
}

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
 *      Do we have any kind of platform integration object in the scene? Probably? because....
 *          It means there would be a comms point for any platform API stuff (if we actually had any) that could be mocked out...
 *          It would disable the player settings on the spawner, making it clear to the customer that those settings would come from the scene 
 *          It would also hold the API point for plugin sync stuff, that's where the connection details would come from! 
 *          It would put a mock in place for the platform UI elements, hub button, global info, etc
 *          It gives us a nice inspector UI for the platform integration requirementds. It could even prevent play mode or at least show a pop up message if these requirements aren't met. It can even have a button to do the bundle process 
 *          We WILL need some platform-specific code shipped to the customer anyway to do the bundling, may as well make it this! 
 *          
 *          So, this PlatformIntegrationProvider is what everything actually talks to. This PlatformIntegrationProvider talks to the "V_PlatformIntegration", which is basically just a proxy/lazy init for the actual platform service
 *          
 *          Ok, so let's have a "V_PlatformIntegration" mono, this is customer facing, and is what the user puts in their scene 
 *          We have a "V_PlatformIntegrationIgnition" mono, this will go under DontDestoyOnLoad. This is the thing that actually creates the service 
 *          On start (or on domain reload!) V_PlatformIntegrationIgnition will instantiate a PlatformService, and store its reference 
 *          When the player or scene syncer request details, they'll request them from V_PlatformIntegration, which will pass this along to PlatformService 
 *          If no PlatformService is found, V_PlatformIntegration will ask PlatformIgnition to create one .... maybe that's better than threading everything through the ignition script... the ignition script should just be for igniting!
 *          
 *          What's the pattern we have for the instance sync stuff?
 *          The state modules just talk to the V_SceneSyncer, effectively, this IS the ignition. That probably makes sense though, the instance sync stuff has ignition config that exists in the scene at design time. 
 *          I mean, a different pattern would be "RelayServerIntegration" and "RelayServerIntegrationIgnition", the VCs talk to "RelayServerIntegration", which talks to "RelayServerIntegrationIgnition" if it doesn't find a server. 
 *          Can we just combine the ignition and the integration?
 *          We could maybe? we could provide different implementations of that ignition object... if we're on platform build, that platform build object actually creates a platform service
 *          If we're in the dev-tools, it just mocks it all out?
 *          No, I don't like that, that then means the script has to behave in totally different ways depending on what context it's in, feels pretty jank 
 *          
 *      classes
 *          Let's have PlatformServiceProvider, that'll be the thing that's private, and persists across scenes. It contains the private platform config, and uses that to create the platform service, needs to boot first
 *          When the player boots, it looks for "PlayerSettingsProvider", this will be V_PlatformIntegration. 
 *          V_PlatformIntegration will search for PlatformServiceProvider, if it finds one, it'll get a reference to the platform service from that. If it DOESN'T find one, it'll just return some mocked stuff, like mock connection details, these can even be in the inspector
 *          
 *          
 *          
 *          
 *      
 *  Right, potential approach 
 *     PlayerSpawner has its own control config. This includes 2d, vr, use settings menu, if settings menu true, show a "OnSettingsChanged" event, and a "SaveSettingsToPlayerPrefs" bool
 *     If a a settings provider is in the scene, all those settings are disabled on the player, and instead live on the settings provider object. This is either some customer thingy, or its the platform's version of the object
 *     Same deal for the network settings. They have their own ConnectionSettings config, but if there's a settings provider on the scene (either custom or platform) then those settings are disabled on the syncer, and instead come from the provider
 *   
 * 
 * 
 */
