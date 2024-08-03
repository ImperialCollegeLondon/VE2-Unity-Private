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
            GameObject frameworkGO = GameObject.Find("ViRSE_Runtime");

            if (frameworkGO != null)
            {
                _frameworkService = frameworkGO.GetComponent<IFrameworkRuntime>();
                HandlePlayerRigReady();
            }
            else
            {
                CreateNewFrameworkRuntime();
            }

            if (serverType != ServerType.Local)
            {
                _pluginSyncService = gameObject.AddComponent<PluginSyncService>();
                _pluginSyncService.Initialize(_frameworkService.PrimaryServerService);
            }

        }

        private void CreateNewFrameworkRuntime()
        {
            GameObject runTimePrefab = Resources.Load<GameObject>("ViRSE_Runtime");
            GameObject runTimeInstance = Instantiate(runTimePrefab);
            DontDestroyOnLoad(runTimeInstance);
            _frameworkService = runTimeInstance.GetComponent<IFrameworkRuntime>();
            _frameworkService.OnFrameworkReady += HandlePlayerRigReady;
            _frameworkService.Initialize(serverType);
            //_frameworkService.LocalPlayerRig.OnPlayerRigReady += HandlePlayerRigReady;
        }

        private void HandlePlayerRigReady()
        {
            _frameworkService.LocalPlayerRig.Position = transform.position;
            _frameworkService.LocalPlayerRig.Rotation = transform.rotation;
        }

        private void OnDestroy()
        {
            _frameworkService.OnFrameworkReady -= HandlePlayerRigReady;
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
