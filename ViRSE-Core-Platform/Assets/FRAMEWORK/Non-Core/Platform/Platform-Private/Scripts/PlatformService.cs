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
using UnityEditor;
using System.Threading;
using System.Linq;
using static ViRSE.Core.Shared.CoreCommonSerializables;

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
    //Ehhh.. there's really not much duplicate code here, not sure we it's worth coupling these two things together
    public class PlatformService : IPlatformService
    {
        private UserIdentity _userIdentity;

        public ushort LocalClientID { get; private set; }
        public string CurrentInstanceCode { get; private set; }
        public GlobalInfo GlobalInfo { get; private set; }
        public event Action<GlobalInfo> OnGlobalInfoChanged;
        public Dictionary<string, WorldDetails> AvailableWorlds { get; private set; }
        public event Action<string> OnInstanceCodeChange;

        /*
                We should probably just be hiding UserSettingsDebug when there IS a platform service?
                Instead, maybe we can just show the platform's user settings directly into the debug thing?

        */

        private IPlatformCommsHandler _commsHandler;

        #region Common-Interfaces
        public bool IsConnectedToServer { get; private set; }
        public event Action OnConnectedToServer;
        #endregion

        #region Instance-Sync-Facing Interfaces
        public InstanceNetworkSettings InstanceNetworkSettings {
            get {
                string currentWorldName = PlatformInstanceInfo.SplitInstanceCode(CurrentInstanceCode).Item1;
                Debug.Log("Looking for instance network settings - current scene is " + currentWorldName);
                if (!AvailableWorlds.TryGetValue(currentWorldName, out WorldDetails worldDetails))
                {
                    Debug.LogError($"Could not find connection world details for world {currentWorldName}");
                    return null;
                }
                else
                {
                    return new InstanceNetworkSettings(worldDetails.IPAddress, worldDetails.PortNumber, CurrentInstanceCode);
                }
            }
        }
        #endregion

        #region Player Settings Interfaces
        public UserSettingsPersistable UserSettings { get; private set; }
        #endregion

        private IPlayerSettingsProvider _playerSettingsProvider;

        #region Platform-Private Interfaces
        public void RequestInstanceAllocation(string worldName, string instanceSuffix)
        {
            if (IsConnectedToServer)
            {
                if (worldName.ToUpper().Equals("HUB") || AvailableWorlds.ContainsKey(worldName))
                {
                    InstanceAllocationRequest instanceAllocationRequest = new(worldName, instanceSuffix);
                    _commsHandler.SendMessage(instanceAllocationRequest.Bytes, PlatformNetworkingMessageCodes.InstanceAllocationRequest, TransmissionProtocol.TCP);
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
        public void RequestHubAllocation()
        {
            RequestInstanceAllocation("Hub", "Solo");
        }   
        #endregion

        public PlatformService(IPlatformCommsHandler commsHandler, UserIdentity userIdentity, IPAddress ipAddress, ushort portNumber, string startingInstanceCode)
        {
            _userIdentity = userIdentity;
            _commsHandler = commsHandler;
            CurrentInstanceCode = startingInstanceCode;

            commsHandler.OnReceiveNetcodeConfirmation += HandleReceiveNetcodeVersion;
            commsHandler.OnReceiveServerRegistrationConfirmation += HandleReceiveServerRegistrationConfirmation;
            commsHandler.OnReceiveGlobalInfoUpdate += HandleReceiveGlobalInfoUpdate;

            //if (_serverType == ServerType.Local)
            //{
            //    //TODO - start local server
            //}

            _commsHandler.ConnectToServer(ipAddress, portNumber);
        }

        public void SetupForNewInstance(IPlayerSettingsProvider playerSettingsProvider)
        {
            if (_playerSettingsProvider != null)
                _playerSettingsProvider.OnPlayerSettingsChanged -= HandleUserSettingsChanged;

            _playerSettingsProvider = playerSettingsProvider;

            if (_playerSettingsProvider != null)
                _playerSettingsProvider.OnPlayerSettingsChanged += HandleUserSettingsChanged;
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
                //Debug.Log("Rec platform nv, sending reg, instance code. " + _currentInstanceCode);

                ServerRegistrationRequest serverRegistrationRequest = new(_userIdentity, CurrentInstanceCode);
                _commsHandler.SendMessage(serverRegistrationRequest.Bytes, PlatformNetworkingMessageCodes.ServerRegistrationRequest, TransmissionProtocol.TCP);
            }
        }

        private void HandleReceiveServerRegistrationConfirmation(byte[] bytes)
        {
            ServerRegistrationConfirmation serverRegistrationConfirmation = new(bytes);

            LocalClientID = serverRegistrationConfirmation.LocalClientID;
            GlobalInfo = serverRegistrationConfirmation.GlobalInfo;
            AvailableWorlds = serverRegistrationConfirmation.AvailableWorlds;
            UserSettings = serverRegistrationConfirmation.UserSettings;

            IsConnectedToServer = true;

            //TODO try catch
            OnConnectedToServer?.Invoke();

            OnGlobalInfoChanged?.Invoke(GlobalInfo);

            //Debug.Log("Local client platform ID = " + _localClientID);
            foreach (WorldDetails worldDetails in AvailableWorlds.Values)
            {
                Debug.Log($"World {worldDetails.Name} at {worldDetails.IPAddress}:{worldDetails.PortNumber}");
            }
        }

        private void HandleReceiveGlobalInfoUpdate(byte[] bytes)
        {
            GlobalInfo newGlobalInfo = new(bytes);

            if (newGlobalInfo.Bytes.SequenceEqual(GlobalInfo.Bytes)) 
                return;

            //TODO - might be better to have the instance allocation be a specific message, rather than being ended implicitely in the global info update?
            //Are we then worried about missing the message?
            PlatformInstanceInfo newLocalInstanceInfo = newGlobalInfo.InstanceInfoForClient(LocalClientID);
            if (newLocalInstanceInfo.InstanceCode != CurrentInstanceCode) 
            {
                HandleInstanceAllocation(newLocalInstanceInfo);
            }

            GlobalInfo = newGlobalInfo;
            OnGlobalInfoChanged?.Invoke(GlobalInfo);
        }

        private void HandleInstanceAllocation(PlatformInstanceInfo newInstanceInfo)
        {
            Debug.Log($"<color=green>Detected allocation to new instance, going to {newInstanceInfo.InstanceCode}</color>");

            CurrentInstanceCode = newInstanceInfo.InstanceCode;
            OnInstanceCodeChange?.Invoke(newInstanceInfo.InstanceCode);

            if (newInstanceInfo.InstanceCode.StartsWith("Hub"))
            {
                SceneManager.LoadScene("Hub");
            }
            //TODO, should be talking to the plugin loader instead here 
            else if (newInstanceInfo.InstanceCode.StartsWith("Dev"))
            {
                SceneManager.LoadScene("Dev");
            }
            else
            {
                Debug.LogError("Couldn't go to scene");
            }
        }

        public void MainThreadUpdate()
        {
            _commsHandler.MainThreadUpdate();
        }

        public void NetworkUpdate()
        {
            //if (ConnectedToServer)
            //{

            //}
        }

        private void ReceivePingFromHost()
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

         private void HandleUserSettingsChanged()
         {
            Debug.Log("Platform detected user settings changed - name = " + UserSettings.PresentationConfig.PlayerName);
             _commsHandler.SendMessage(UserSettings.Bytes, PlatformNetworkingMessageCodes.UpdateUserSettings, TransmissionProtocol.TCP);
         }

        public void TearDown()
        {
            _commsHandler?.DisconnectFromServer();
            //Probably destroy remote players, so we can create them from fresh after domain reload 
        }
    }

    public static class GlobalInfoExtensions
    {
        public static PlatformInstanceInfo InstanceInfoForClient(this GlobalInfo globalInfo, ushort localClientID)
        {
            foreach (PlatformInstanceInfo platformInstanceInfo in globalInfo.InstanceInfos.Values)
            {
                if (platformInstanceInfo.ClientInfos.ContainsKey(localClientID))
                    return platformInstanceInfo;
            }

            return null;
        }
    }
}

