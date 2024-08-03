using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViRSE.FrameworkRuntime;
using ViRSE.FrameworkRuntime.LocalPlayerRig;

namespace ViRSE.PluginRuntime
{
    //TODO, I do think we should probably split this, so all this script does is create the PluginService, and pass some config
    public class ViRSEManager : MonoBehaviour //Basically, "PluginRuntime"
    {
        public static ViRSEManager Instance;

        [SerializeField] private ServerType serverType;
        public ServerType ServerType => serverType;

        //Refences to framework interfaces
        private IFrameworkRuntime _frameworkService;

        //References to the Plugin Subservices
        private PluginSyncService _pluginSyncService;


        //private PluginPrimaryUIService _pluginPrimaryUIService
        //private PluginSecondaryUIService _pluginSecondaryUIService

        void Awake()
        {
            Instance = this;

            GameObject frameworkGO = GameObject.Find("ViRSE_Runtime");

            if (frameworkGO != null)
            {
                _frameworkService = frameworkGO.GetComponent<IFrameworkRuntime>();
                HandleFrameworkReady();
            }
            else
            {
                _frameworkService = CreateNewFrameworkRuntime();
                _frameworkService.OnFrameworkReady += HandleFrameworkReady;
                _frameworkService.Initialize(serverType);
            }
        }

        private IFrameworkRuntime CreateNewFrameworkRuntime()
        {
            GameObject runTimePrefab = Resources.Load<GameObject>("ViRSE_Runtime");
            GameObject runTimeInstance = Instantiate(runTimePrefab);
            DontDestroyOnLoad(runTimeInstance);

            IFrameworkRuntime frameworkService = runTimeInstance.GetComponent<IFrameworkRuntime>();
            return frameworkService;
        }

        private void HandleFrameworkReady()
        {
            ConfigurePlayerForSpawn();
            SetupPluginSyncService();
        }

        private void ConfigurePlayerForSpawn()
        {
            Debug.Log("fwork " + _frameworkService.ToString());
            Debug.Log("LPR " + _frameworkService.LocalPlayerRig.ToString());
            _frameworkService.LocalPlayerRig.Position = transform.position;
            _frameworkService.LocalPlayerRig.Rotation = transform.rotation;
        }

        private void SetupPluginSyncService()
        {
            if (serverType == ServerType.Offline)
            {

            }
            else
            {
                _pluginSyncService = gameObject.AddComponent<PluginSyncService>();
                _pluginSyncService.Initialize(_frameworkService.PrimaryServerService);
            }
        }

        private void OnDestroy()
        {
            _frameworkService.OnFrameworkReady -= HandleFrameworkReady;
        }
    }
}

/*
 * There should probably be some OnFrameworkReady event 
 * Actually...
 * Why don't we just have a FrameworkRuntime.RegisterPlugin call
 * If framework exists, register plugin straight away 
 * Otherwise, register later...
 * Thing is, I want this event fired off either way 
 * Ok, so first, we register with the plugin, plugin then returns an event?
 * No that doesn't work either, how does the event get invoked after its returned?
 * 
 * 
 */

//Maybe we should split out the bit that actually instantiates the framework?
/*Whatever script THAT is can relay a "OnFrameworkReady" event?
 * 
 * 
 */
