using System.Net;
using UnityEngine;
using ViRSE.Core.Player;
using ViRSE.Core.Shared;
using ViRSE.PluginRuntime;
using ViRSE.PluginRuntime.VComponents;
using static ViRSE.Core.Shared.CoreCommonSerializables;

//TODO, we'll need to follow a same approach to core <-> instance
//We'll need some interface for the platform integration, this script should look for that, if there is one 
//We'll need to disable "ConnectAutomatically"

namespace ViRSE.InstanceNetworking
{
    [ExecuteInEditMode]
    public class V_SceneSyncer : MonoBehaviour, INetworkManager
    {
        //TODO hide this if platform integration isn't present 
        [SerializeField] public bool ConnectAutomatically = true; //If false, connection must be programmatic 

        [SerializeField] private string _instanceCode = "dev"; //Can be changed by the customer code(?) 

        //If isPlatform, these settings will be overriden by whatever the platform says
        [SerializeField] private ServerType _serverType = ServerType.Local;
        [SerializeField] private string _localServerIP = "127.0.0.1";
        [SerializeField] private string _remoteServerIP = "";
        [SerializeField] private int portNumber = 4297;

        private PluginSyncService _pluginSyncService;
        private PluginSyncService PluginSyncService 
        {
            get {
                if (_pluginSyncService == null)
                    Awake();

                return _pluginSyncService;
            }
            set {
                _pluginSyncService = value;
            }
        }

        private void Awake()
        {
            if (_pluginSyncService != null)
                return;

            Debug.Log("SCENE SYNCER AWAKE a!");

            if (_serverType != ServerType.Offline)
            {
                PlayerPresentationConfig playerPresentationConfig = null;
                GameObject playerSpawner = GameObject.Find("PlayerSpawner");
                if (playerSpawner != null)
                {
                    playerPresentationConfig = playerSpawner.GetComponent<V_PlayerSpawner>().PresentationConfig;
                }

                _pluginSyncService = PluginSyncServiceFactory.Create(playerPresentationConfig);
            }

            if (ConnectAutomatically) //And we're not on the platform!
            {
                ConnectToServer();
            }
        }

        //The platform will call this with overrides
        public void ConnectToServer(string ipAddressOverride = null, int portNumberOverride = -1, string instanceCodeOverride = null)
        {
            string ipAddressString;
            
            if (ipAddressOverride == null)
                ipAddressString = _serverType == ServerType.Local ? _localServerIP : _remoteServerIP;
            else
                ipAddressString = ipAddressOverride;

            if (portNumberOverride == -1)
                portNumberOverride = portNumber;

            if (instanceCodeOverride != null)
                _instanceCode = instanceCodeOverride;

            if (IPAddress.TryParse(ipAddressString, out IPAddress ipAddress) == false)
                throw new System.Exception("Invalid IP address, could not connect to server");

            PluginSyncService.ConnectToServer(ipAddress, portNumber, _instanceCode);
        }

        private void FixedUpdate()
        {
            PluginSyncService.NetworkUpdate();
        }

        public void RegisterStateModule(IStateModule stateModule, string stateType, string goName)
        {
            PluginSyncService.RegisterStateModule(stateModule, stateType, goName);
        }

        //TODO, API to change instance code, and to connect/disconnect
        //API for connect, disconnect, change instance code 

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                BaseStateHolder[] baseStateConfigs = GameObject.FindObjectsOfType<BaseStateHolder>();

                foreach (BaseStateHolder baseStateConfig in baseStateConfigs)
                    baseStateConfig.BaseStateConfig.NetworkManager = this;

                return;
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                BaseStateHolder[] baseStateConfigs = GameObject.FindObjectsOfType<BaseStateHolder>();

                foreach (BaseStateHolder baseStateConfig in baseStateConfigs)
                    baseStateConfig.BaseStateConfig.NetworkManager = null;
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

