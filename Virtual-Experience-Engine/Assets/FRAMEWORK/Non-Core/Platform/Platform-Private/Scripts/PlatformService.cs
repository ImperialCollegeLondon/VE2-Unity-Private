using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using static NonCoreCommonSerializables;
using UnityEngine.SceneManagement;
using System.Linq;
using VE2.PlatformNetworking;
using static VE2.PlatformNetworking.PlatformSerializables;
using static VE2.Common.CommonSerializables;
using VE2.Common;

namespace VE2.NonCore.Platform.Private
{
    public static class PlatformServiceFactory
    {
        //Shouldn't pass these details,
        //Should pass these in the "Connect" method
        public static PlatformService Create()
        {
            PlatformCommsHandler commsHandler = new(new DarkRift.Client.DarkRiftClient());
            return new PlatformService(commsHandler, new PluginLoader(), VE2CoreServiceLocator.Instance.PlayerSettingsHandler);
        }
    }

    //TODO, we might want to consider cutting down the duplicate code with the instance sync stuff
    //Ehhh.. there's really not much duplicate code here, not sure we it's worth coupling these two things together
    public class PlatformService : IPlatformService
    {
        public ushort LocalClientID { get; private set; }
        public string CurrentInstanceCode { get; private set; } //Remove, comes from settingshandler?
        public GlobalInfo GlobalInfo { get; private set; }
        public event Action<GlobalInfo> OnGlobalInfoChanged;
        public Dictionary<string, WorldDetails> ActiveWorlds { get; private set; }
        public event Action<string> OnInstanceCodeChange;

        /*
                We should probably just be hiding UserSettingsDebug when there IS a platform service?
                Instead, maybe we can just show the platform's user settings directly into the debug thing?

        */

        #region Common-Interfaces
        public bool IsConnectedToServer { get; private set; }
        public event Action OnConnectedToServer;
        #endregion

        #region Platform-Private Interfaces
        public void RequestInstanceAllocation(string worldName, string instanceSuffix)
        {
            if (IsConnectedToServer)
            {
                Debug.Log($"Requesting instance allocation to {worldName}-{instanceSuffix}");
                InstanceAllocationRequest instanceAllocationRequest = new(worldName, instanceSuffix);
                _commsHandler.SendMessage(instanceAllocationRequest.Bytes, PlatformNetworkingMessageCodes.InstanceAllocationRequest, TransmissionProtocol.TCP);
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

        private IPlatformCommsHandler _commsHandler;
        private readonly PluginLoader _pluginLoader;
        private readonly IPlayerSettingsHandler _playerSettingsProvider;

        public PlatformService(IPlatformCommsHandler commsHandler, PluginLoader pluginLoader, IPlayerSettingsHandler playerSettingsProvider)
        {
            _commsHandler = commsHandler;
            _pluginLoader = pluginLoader;
            _playerSettingsProvider = playerSettingsProvider;
            _playerSettingsProvider.OnPlayerPresentationConfigChanged += HandlePlayerPresentationConfigChanged;

            commsHandler.OnReceiveNetcodeConfirmation += HandleReceiveNetcodeVersion;
            commsHandler.OnReceiveServerRegistrationConfirmation += HandleReceiveServerRegistrationConfirmation;
            commsHandler.OnReceiveGlobalInfoUpdate += HandleReceiveGlobalInfoUpdate;


            if (_playerSettingsProvider != null)
                _playerSettingsProvider.OnPlayerPresentationConfigChanged += HandlePlayerPresentationConfigChanged;

            //_commsHandler.ConnectToServer(ipAddress, portNumber);
        }

        public void ConnectToPlatform(IPAddress ipAddress, ushort portNumber, string startingInstanceCode)
        {
            CurrentInstanceCode = startingInstanceCode;
            _commsHandler.ConnectToServer(ipAddress, portNumber);
        }

        // public void SetupForNewInstance(IPlayerSettingsProvider playerSettingsProvider)
        // {
        //     if (_playerSettingsProvider != null)
        //         _playerSettingsProvider.OnLocalChangeToPlayerSettings -= HandleUserSettingsChanged;

        //     _playerSettingsProvider = playerSettingsProvider;

        //     if (_playerSettingsProvider != null)
        //         _playerSettingsProvider.OnLocalChangeToPlayerSettings += HandleUserSettingsChanged;
        // }

        private void HandleReceiveNetcodeVersion(byte[] bytes)
        {
            NetcodeVersionConfirmation netcodeVersionConfirmation = new(bytes);

            if (netcodeVersionConfirmation.NetcodeVersion != PlatformNetcodeVersion)
            {
                Debug.LogError($"Bad platform netcode version, received version {netcodeVersionConfirmation.NetcodeVersion} but we are on {PlatformNetcodeVersion}");
            } 
            else
            {
                //if first time auth connect, do nothing? 
                //Otherwise, send reg request to platform

                //string correctedInstanceCode = CurrentInstanceCode.Contains("Hub") ? "Hub-Solo" : CurrentInstanceCode;
                //ServerRegistrationRequest serverRegistrationRequest = new(_userIdentity, CurrentInstanceCode);
                //_commsHandler.SendMessage(serverRegistrationRequest.Bytes, PlatformNetworkingMessageCodes.ServerRegistrationRequest, TransmissionProtocol.TCP);
            }
        }

        private void HandleReceiveServerRegistrationConfirmation(byte[] bytes)
        {
            ServerRegistrationConfirmation serverRegistrationConfirmation = new(bytes);

            LocalClientID = serverRegistrationConfirmation.LocalClientID;
            GlobalInfo = serverRegistrationConfirmation.GlobalInfo;

            ActiveWorlds = serverRegistrationConfirmation.AvailableWorlds;
            //How do we get the FTP settings into the next scene?
            //Don't really want to have to ask the server for them each time?
            //Really, it should come in through arguments.. but, we also need to be able to start in that scene?

            //No, just have hasArgs = false by default, if so, fall back to that scenes default settings
            //BUT - that means when we do connect to prod from editor, it'll go to fallback settings, even if the server WOULD give us real ones, if we waited 

            //Let's just say the framework ftp settings get populated in the introscene, same with the customer key 
            //The worlds can come from the request from the hub - although, we need these in plugins too? so maybe world FTP and IPs come from the server each time?

            //No, let's actually get the worldSubStore FTP and instancing IP from the hub only, explicit request 

            IsConnectedToServer = true;

            

            OnConnectedToServer?.Invoke();
            OnGlobalInfoChanged?.Invoke(GlobalInfo);
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
            else 
            {
                _pluginLoader.LoadPlugin(newInstanceInfo.WorldName, int.Parse(newInstanceInfo.InstanceSuffix));
            }
            // else if (newInstanceInfo.InstanceCode.StartsWith("Dev"))
            // {
            //     SceneManager.LoadScene("Dev");
            // }
            // else
            // {
            //     Debug.LogError("Couldn't go to scene");
            // }
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

         private void HandlePlayerPresentationConfigChanged(PlayerPresentationConfig playerPresentationConfig)
         {
             _commsHandler.SendMessage(playerPresentationConfig.Bytes, PlatformNetworkingMessageCodes.UpdatePlayerPresentation, TransmissionProtocol.TCP);
         }

        public void TearDown()
        {
            _commsHandler?.DisconnectFromServer();

            if (_playerSettingsProvider != null)
                _playerSettingsProvider.OnPlayerPresentationConfigChanged += HandlePlayerPresentationConfigChanged;
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

