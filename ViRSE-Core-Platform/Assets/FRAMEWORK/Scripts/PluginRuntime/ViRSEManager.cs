using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViRSE.FrameworkRuntime;
using ViRSE.FrameworkRuntime.LocalPlayerRig;

namespace ViRSE.PluginRuntime
{
    //TODO, I do think we should probably split this, so all this script does is create the PluginService, and pass some config
    [AddComponentMenu("Scripts/V_ViRSE Manager")]
    public class ViRSEManager : MonoBehaviour //Basically, "PluginRuntime"
    {
        public static ViRSEManager Instance;

        [SerializeField] private ServerType serverType;
        public ServerType ServerType => serverType;

        //Refences to framework interfaces
        private IFrameworkRuntime _frameworkRuntime;

        //References to the Plugin Subservices
        private PluginSyncService _pluginSyncService;


        //private PluginPrimaryUIService _pluginPrimaryUIService
        //private PluginSecondaryUIService _pluginSecondaryUIService

        void Awake()
        {
            Instance = this;

            GameObject frameworkGO = GameObject.Find("ViRSE_Runtime");
            if (frameworkGO != null)
                _frameworkRuntime = frameworkGO.GetComponent<IFrameworkRuntime>();
            else
                _frameworkRuntime = CreateNewFrameworkRuntime();

            _frameworkRuntime.Initialize(ServerType);

            if (serverType != ServerType.Offline)
            {
                _pluginSyncService = gameObject.AddComponent<PluginSyncService>();
                _pluginSyncService.Initialize(_frameworkRuntime.PrimaryServerService);
            }

            if (_frameworkRuntime.IsFrameworkReady)
                HandleFrameworkReady();
            else
                _frameworkRuntime.OnFrameworkReady += HandleFrameworkReady;
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
            MovePlayerToPluginStartingPosition();
        }

        private void MovePlayerToPluginStartingPosition()
        {
            _frameworkRuntime.LocalPlayerRig.Position = transform.position;
            _frameworkRuntime.LocalPlayerRig.Rotation = transform.rotation;
        }

        private void OnDestroy()
        {
            _frameworkRuntime.OnFrameworkReady -= HandleFrameworkReady;
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
