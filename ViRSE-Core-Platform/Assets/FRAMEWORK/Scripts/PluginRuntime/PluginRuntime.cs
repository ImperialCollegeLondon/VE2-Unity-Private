using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViRSE.FrameworkRuntime;

namespace ViRSE.PluginRuntime
{
    //TODO - this handles setting up the player, and setting up the plugin networking stuff, maybe these should be separated out? Both would need a server type
    public class PluginRuntime
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
