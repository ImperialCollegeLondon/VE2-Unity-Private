using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using UnityEngine;
using UnityEngine.Events;
using ViRSE.FrameworkRuntime;
using ViRSE.Core.Shared;
using System.Net;
using static NonCoreCommonSerializables;
using static PlatformSerializables;
using UnityEngine.SceneManagement;
namespace ViRSE.PluginRuntime
{
    public static class PlatformServiceFactory
    {
        public static IPlatformService Create(UserIdentity userIdentity, IPAddress ipAddress, ushort portNumber, string startingInstanceCode)
        {
            PlatformCommsHandler commsHandler = new(new DarkRift.Client.DarkRiftClient());
            return new PlatformService(commsHandler, userIdentity, ipAddress, portNumber, startingInstanceCode);
        }
    }

    //TODO, we might want to consider cutting down the duplicate code with the instance sync stuff
    public class PlatformService : IPlatformService
    {
        private UserIdentity _userIdentity;

        private ushort _localClientID;
        private string _currentInstanceCode;
        private GlobalInfo _GlobalInfo;
        private Dictionary<string, WorldDetails> _availableWorlds;

        private IPlatformCommsHandler _commsHandler;

        #region Common-Interfaces
        public bool IsConnectedToServer { get; private set; }
        public event Action OnConnectedToServer;
        #endregion

        #region Instance-Sync-Facing Interfaces
        private InstanceNetworkSettings _instanceNetworkSettings;
        public InstanceNetworkSettings InstanceNetworkSettings {
            get {

                if (_instanceNetworkSettings == null)
                {
                    string currentSceneName = SceneManager.GetActiveScene().name;
                    if (!_availableWorlds.TryGetValue(currentSceneName, out WorldDetails worldDetails))
                        Debug.LogError($"Could not find connection world details for world {currentSceneName}");
                    else
                        _instanceNetworkSettings = new InstanceNetworkSettings(worldDetails.IPAddress, worldDetails.PortNumber, _currentInstanceCode);
                }
                return _instanceNetworkSettings;
            }
        }
        #endregion

        #region Player-Rig-Facing Interfaces
        public UserSettings UserSettings { get; private set; }
        #endregion

        #region Platform-Private Interfaces
        public void RequestInstanceAllocation(string worldName, string instanceSuffix)
        {
            if (IsConnectedToServer)
            {
                if (_availableWorlds.ContainsKey(worldName))
                {
                    InstanceAllocationRequest instanceAllocationRequest = new(worldName, instanceSuffix);
                    _commsHandler.SendInstanceAllocationRequest(instanceAllocationRequest.Bytes);
                }
                else
                {
                    Debug.LogError($"Could not find world details for world {worldName}");
                }
            }
            else
            {
                Debug.LogError("Not yet connected to server");
            }
        }
        #endregion

        public PlatformService(IPlatformCommsHandler commsHandler, UserIdentity userIdentity, IPAddress ipAddress, ushort portNumber, string startingInstanceCode)
        {
            _userIdentity = userIdentity;
            _commsHandler = commsHandler;
            _currentInstanceCode = startingInstanceCode;

            commsHandler.OnReceiveNetcodeConfirmation += HandleReceiveNetcodeVersion;
            commsHandler.OnReceiveServerRegistrationConfirmation += HandleReceiveServerRegistrationConfirmation;

            //if (_serverType == ServerType.Local)
            //{
            //    //TODO - start local server
            //}

            _commsHandler.ConnectToServer(ipAddress, portNumber);
        }

        private void HandleReceiveNetcodeVersion(byte[] bytes)
        {
            NetcodeVersionConfirmation netcodeVersionConfirmation = new(bytes);

            if (netcodeVersionConfirmation.NetcodeVersion != PlatformNetcodeVersion)
            {
                //TODO - handle bad netcode version
                Debug.LogError($"Bad platform netcode version, received version {netcodeVersionConfirmation.NetcodeVersion} but we are on {PlatformNetcodeVersion}");
            } 
            else
            {
                Debug.Log("Rec platform nv, sending reg, instance code. " + _currentInstanceCode);

                ServerRegistrationRequest serverRegistrationRequest = new(_userIdentity, _currentInstanceCode);
                _commsHandler.SendServerRegistrationRequest(serverRegistrationRequest.Bytes);
            }
        }

        private void HandleReceiveServerRegistrationConfirmation(byte[] bytes)
        {
            ServerRegistrationConfirmation serverRegistrationConfirmation = new(bytes);

            _localClientID = serverRegistrationConfirmation.LocalClientID;
            _GlobalInfo = serverRegistrationConfirmation.GlobalInfo;
            _availableWorlds = serverRegistrationConfirmation.AvailableWorlds;
            UserSettings = new(serverRegistrationConfirmation.PlayerPresentationConfig, serverRegistrationConfirmation.PlayerVRControlConfig, serverRegistrationConfirmation.Player2DControlConfig);

            IsConnectedToServer = true;

            //TODO try catch
            OnConnectedToServer?.Invoke(); //TODO, this is crapping out

            Debug.Log("Local client platform ID = " + _localClientID);
            foreach (WorldDetails worldDetails in _availableWorlds.Values)
            {
                Debug.Log($"World {worldDetails.Name} at {worldDetails.IPAddress}:{worldDetails.PortNumber}");
            }
        }

        private void HandleReceiveGlobalInfoUpdate(byte[] bytes)
        {
            GlobalInfo newGlobalInfo = new(bytes);

            //Handle all the things! Mainly, instance allocations
        }

        public void NetworkUpdate()
        {
            //if (ConnectedToServer)
            //{

            //}
        }

        public void ReceivePingFromHost()
        {
            //TODO calc buffer size
        }

        //TODO, create platform UI, although maybe this shouldn't be here?
        /*  Maybe the main UI should search for "UIProviders"?
         *  Idk, doesn't really feel like it should be the UIs job to search for and obtain all its sub-UI panels,
         *  equally doesn't really feel like it should be the platform service's job to detect scene load and create UIs too though...
         *  Maybe this should be V_PlatformIntegration's job?
         * 
         * 
         */

        public void TearDown()
        {
            _commsHandler?.DisconnectFromServer();
            //Probably destroy remote players?
        }
    }
}
