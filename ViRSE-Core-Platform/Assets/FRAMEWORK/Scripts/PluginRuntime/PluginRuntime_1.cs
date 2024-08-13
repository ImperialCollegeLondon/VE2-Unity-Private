using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViRSE.FrameworkRuntime;

namespace ViRSE.PluginRuntime
{
    //Maybe this should hold the player config and network config (and maybe also platform config) as separate configs, and create each of those elements separately 
    public class V_Ignition : MonoBehaviour
    {
        [SerializeField] private ServerType serverType;
        public ServerType ServerType => serverType;

        private PluginRuntime _pluginRuntime;
        private IFrameworkRuntime _frameworkRuntime;

        private void Start()
        {
            GameObject frameworkGO = GameObject.Find("ViRSE_Runtime");

            if (frameworkGO == null)
            {
                _frameworkRuntime = CreateNewFrameworkRuntime();
                _frameworkRuntime.Initialize(serverType);
            }

            _pluginRuntime = new(_frameworkRuntime, serverType, transform);
        }

        private IFrameworkRuntime CreateNewFrameworkRuntime()
        {
            GameObject runTimePrefab = Resources.Load<GameObject>("ViRSE_Runtime");
            GameObject frameworkGO = GameObject.Instantiate(runTimePrefab);
            DontDestroyOnLoad(frameworkGO);

            return frameworkGO.GetComponent<ViRSERuntime>();
        }

        private void FixedUpdate()
        {
            _pluginRuntime.HandleNetworkUpdate();
        }

        private void OnDestroy()
        {
            _pluginRuntime.TearDown();
        }
    }


    //TODO - this handles setting up the player, and setting up the plugin networking stuff, maybe these should be separated out? Both would need a server type
    [AddComponentMenu("Scripts/V_ViRSE Manager")]
    public class PluginRuntime //Basically, "PluginRuntime"
    {
        public static PluginRuntime Instance;

        //Refences to framework interfaces
        private IFrameworkRuntime _frameworkRuntime;

        //References to the Plugin Subservices
        private PluginSyncService _pluginSyncService;

       public ServerType ServerType;


        //private PluginPrimaryUIService _pluginPrimaryUIService
        //private PluginSecondaryUIService _pluginSecondaryUIService

        private Transform _playerSpawnTransform;

        public PluginRuntime(IFrameworkRuntime frameworkRuntime, ServerType serverType, Transform playerSpawnTransform)
        {
            Instance = this;
            ServerType = serverType;
            _frameworkRuntime = frameworkRuntime;

            if (serverType != ServerType.Offline)
                _pluginSyncService = new(frameworkRuntime.PrimaryServerService);

            if (frameworkRuntime.IsFrameworkReady)
                HandleFrameworkReady();
            else
                frameworkRuntime.OnFrameworkReady += HandleFrameworkReady;

            _playerSpawnTransform = playerSpawnTransform;
        }

        private void HandleFrameworkReady()
        {
            MovePlayerToPluginStartingPosition();
        }

        private void MovePlayerToPluginStartingPosition()
        {
            _frameworkRuntime.LocalPlayerRig.Position = _playerSpawnTransform.position;
            _frameworkRuntime.LocalPlayerRig.Rotation = _playerSpawnTransform.rotation;
        }

        public void HandleNetworkUpdate()
        {
            _pluginSyncService.HandleNetworkUpdate();
        }

        public void TearDown()
        {
            _frameworkRuntime.OnFrameworkReady -= HandleFrameworkReady;
            _pluginSyncService.TearDown();
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
