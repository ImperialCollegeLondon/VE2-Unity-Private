using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using ViRSE.Core.Shared;
using ViRSE.PluginRuntime;
using static PlatformNetworkObjects;

namespace ViRSE.InstanceNetworking
{
    [ExecuteInEditMode]
    public class V_PlatformIntegration : MonoBehaviour //will need some customer-facing interfacing 
    {
        [SerializeField] private bool _connectAutomatically = true; //If false, connection must be programmatic 

        [SerializeField] private string _startingInstanceSuffix = "dev"; //Can be changed by the customer code(?) 

        //If isPlatform, these settings will be overriden by whatever the platform says
        [SerializeField] private ServerType _serverType = ServerType.Local;
        [SerializeField] private string _localServerIP = "127.0.0.1";
        [SerializeField] private string _remoteServerIP = "";
        [SerializeField] private int portNumber = 4296;

        [SerializeField] private bool UseSpoofUserIdentity = false;
        [SerializeField] string spoofDomain;
        [SerializeField] string spoofAccountID;

        private PlatformService _platformService;
        private PlatformService PlatformServuce  //We'll add platform components to the scene on start (e.g global info UI), this will let those components access the platform after domain reload
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

        private void Awake()
        {
            if (_platformService != null)
                return;

            Debug.Log("Platform syncer awake!");

            if (_serverType != ServerType.Offline)
            {
                _platformService = PlatformServiceFactory.Create(GetUserIdentity());
                ConnectToServer();
            }
        }

        private UserIdentity GetUserIdentity()
        {
            if (UseSpoofUserIdentity && Application.isEditor)
            {
                return new UserIdentity(spoofDomain, spoofAccountID);
            }
            else
            {
                //Read user ident from command line if there's stuff to read
                return new UserIdentity("test", "guest");
            }
        }

        //The overrides here will come from cmd args from the launcher
        public void ConnectToServer(string ipAddressOverride = null, int portNumberOverride = -1)
        {
            string ipAddressString;
            
            if (ipAddressOverride == null)
                ipAddressString = _serverType == ServerType.Local ? _localServerIP : _remoteServerIP;
            else
                ipAddressString = ipAddressOverride;

            if (portNumberOverride == -1)
                portNumberOverride = portNumber;

            if (IPAddress.TryParse(ipAddressString, out IPAddress ipAddress) == false)
                throw new System.Exception("Invalid IP address, could not connect to server");

            string instanceCodeFull = SceneManager.GetActiveScene().name + "-" + _startingInstanceSuffix;

            PlatformServuce.ConnectToServer(ipAddress, portNumber, instanceCodeFull);
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
        }


//#if UNITY_EDITOR
//            [UnityEditor.InitializeOnLoadMethod]
//        private static void RegisterDomainReloadCallback()
//        {
//            UnityEditor.AssemblyReloadEvents.afterAssemblyReload += HandleReload;
//            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += HandlePreReload;
//        }
//#endif
    }
}

//How should this work here
/*  If we're on the platform, we need the relay IP address to come from the platform (or from a launch argument)
 *  We NEED some don't destroy on load object that holds the IP address to the platform?
 *  Or do we? If it's inside a launch argument... in that case it'd have to go somewhere else 
 *  Anyway, we don't want to drop platform connection every time we change scene 
 *  
 *  Yeah, we'll need some DontDestroyOnLoad platform connection thing, this just needs to keep all its data serialized so that it can continue to connect to the platform
 *  
 *  Now, "platform integration"....... this has the API for file storage.. so it's going to need thingies beyond just the "I am for the platform flag!"
 *  Question is then, how much of the platform stuff is going to function in the editor?
 *  
 *  Platform build only 
 *  Instance switching 
 *  
 *  Build and editor 
 *  FTP services 
 *  Voice chat?
 * 
 * Ok, so, we need an IP address that points towards an FTP server, and maybe some Vivox server, but that gets overriden by the platform
 * So, if in editor, use IP addresses from inspector. If in standalone launcher build, comes from launch args, if in platform plugin build, comes from platform integration thing 
 * Right, so there's some persistent platform integration object that reads CMD arguments, that comes from the launcher. 
 * That platform integration object then survives into other scenes. It looks for other platform integration objects, if it finds one, it destroys it. 
 * 
 * 
 */

//using UnityEngine;

//#if UNITY_EDITOR
//using UnityEditor;
//#endif

//public class SimulateLaunchArguments : MonoBehaviour
//{
//    [SerializeField] private string[] simulatedArgs;

//    private void Awake()
//    {
//#if UNITY_EDITOR
//        // Simulate command-line arguments in the Editor
//        System.Environment.SetEnvironmentVariable("UNITY_SIMULATED_ARGS", string.Join(" ", simulatedArgs));
//        EditorPrefs.SetString("UNITY_SIMULATED_ARGS", string.Join(" ", simulatedArgs));
//#endif
//    }
//}

