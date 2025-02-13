using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using static NonCoreCommonSerializables;
using UnityEngine.SceneManagement;
using System.Linq;
using VE2.PlatformNetworking;
using static VE2.Common.CommonSerializables;
using VE2.Common;
using static VE2.Platform.Internal.PlatformSerializables;
using static VE2.Platform.API.PlatformPublicSerializables;

namespace VE2.NonCore.Platform.Private
{
    public static class PlatformServiceFactory
    {
        //Shouldn't pass these details,
        //Should pass these in the "Connect" method
        public static PlatformService Create()
        {
            PlatformCommsHandler commsHandler = new(new DarkRift.Client.DarkRiftClient());
            return new PlatformService(commsHandler, new PluginLoader(), PlayerLocator.Instance.PlayerSettingsHandler);
        }
    }

    //So we need to expose this stuff to the hub... and we don't want the hub to live in the platform namespace, because then it gets pulled in by plugins 
    //So PlatformService needs to implement two interface, IPlatformAPI and IPlatformAPIPrivate 
    //Goes into ServiceLocator by IPlatformAPI
    //The hub finds it, and casts it to IPlatformAPIPrivate 

    public class PlatformService //: IPlatformService
    {
        public ushort LocalClientID { get; private set; }
        public string CurrentInstanceCode { get; private set; } //TODO: Remove, comes from settingshandler?
        public GlobalInfo GlobalInfo { get; private set; }
        public event Action<GlobalInfo> OnGlobalInfoChanged;
        public Dictionary<string, WorldDetails> ActiveWorlds { get; private set; }

        private ServerConnectionSettings _worldBuildsFTPServerSettings;
        private ServerConnectionSettings _defaultWorldSubStoreFTPServerSettings;
        private ServerConnectionSettings __defaultInstancingServerSettings;

        public event Action<string> OnInstanceCodeChange;

        /*
                We should probably just be hiding UserSettingsDebug when there IS a platform service?
                Instead, maybe we can just show the platform's user settings directly into the debug thing?

        */

        #region Interfaces
        public bool IsConnectedToServer { get; private set; }
        public event Action OnConnectedToServer;
        
        public bool IsAuthFailed { get; private set; }
        public event Action OnAuthFailed;

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

        public ServerConnectionSettings GetInstanceServerSettingsForWorld(string worldName)
        {
            if (ActiveWorlds == null || !ActiveWorlds.ContainsKey(worldName) || !ActiveWorlds[worldName].HasCustomInstanceServer)
                return __defaultInstancingServerSettings;
            else
                return ActiveWorlds[worldName].CustomInstanceServerSettings;
        }

        public ServerConnectionSettings GetInstanceServerSettingsForCurrentWorld() => GetInstanceServerSettingsForWorld(SceneManager.GetActiveScene().name); //TODO: Should come from settings?
        #endregion

        private IPlatformCommsHandler _commsHandler;
        private readonly PluginLoader _pluginLoader;
        private readonly IPlayerSettingsHandler _playerSettingsHandler;

        public PlatformService(IPlatformCommsHandler commsHandler, PluginLoader pluginLoader, IPlayerSettingsHandler playerSettingsProvider)
        {
            _commsHandler = commsHandler;
            _pluginLoader = pluginLoader;
            _playerSettingsHandler = playerSettingsProvider;

            if (_playerSettingsHandler != null)
                _playerSettingsHandler.OnPlayerPresentationConfigChanged += HandlePlayerPresentationConfigChanged;

            commsHandler.OnReceiveNetcodeConfirmation += HandleReceiveNetcodeVersion;
            commsHandler.OnReceiveServerRegistrationConfirmation += HandleReceiveServerRegistrationResponse;
            commsHandler.OnReceiveGlobalInfoUpdate += HandleReceiveGlobalInfoUpdate;
        }

        public void ConnectToPlatform(IPAddress ipAddress, ushort portNumber, string startingInstanceCode)
        {
            Debug.Log("Connecting to platform");
            CurrentInstanceCode = startingInstanceCode;
            _commsHandler.ConnectToServer(ipAddress, portNumber);
        }

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

                string correctedInstanceCode = CurrentInstanceCode.Contains("Hub") ? "Hub-Solo" : CurrentInstanceCode; //TODO: Figure out instance code
                string customerID = "test", customerKey = "test"; //TODO - figure out these too!

                ServerRegistrationRequest serverRegistrationRequest = new(customerID, customerKey, CurrentInstanceCode, _playerSettingsHandler.PlayerPresentationConfig);
                _commsHandler.SendMessage(serverRegistrationRequest.Bytes, PlatformNetworkingMessageCodes.ServerRegistrationRequest, TransmissionProtocol.TCP);
            }
        }

        private void HandleReceiveServerRegistrationResponse(byte[] bytes)
        {
            ServerRegistrationResponse serverRegistrationConfirmation = new(bytes);

            //TODO: Check if auth success

            LocalClientID = serverRegistrationConfirmation.LocalClientID;
            GlobalInfo = serverRegistrationConfirmation.GlobalInfo;
            ActiveWorlds = serverRegistrationConfirmation.ActiveWorlds;

            IsConnectedToServer = true;

            Debug.Log("Receive reg response from server " + LocalClientID);

            OnConnectedToServer?.Invoke();
            OnGlobalInfoChanged?.Invoke(GlobalInfo);
        }

        private void HandleReceiveGlobalInfoUpdate(byte[] bytes)
        {
            GlobalInfo newGlobalInfo = new(bytes);

            Debug.Log("Rec global update 1");

            if (GlobalInfo != null && newGlobalInfo.Bytes.SequenceEqual(GlobalInfo.Bytes))
                return;

            //TODO - might be better to have the instance allocation be a specific message, rather than being ended implicitely in the global info update?
            //Are we then worried about missing the message?
            PlatformInstanceInfo newLocalInstanceInfo = newGlobalInfo.InstanceInfoForClient(LocalClientID);
            Debug.Log("Received global info update... our ID " + LocalClientID);
            foreach (PlatformInstanceInfo instanceInfo in newGlobalInfo.InstanceInfos.Values)
            {
                Debug.Log($"Instance {instanceInfo.InstanceCode} has {instanceInfo.ClientInfos.Count}");
                foreach (PlatformClientInfo platformClientInfo in instanceInfo.ClientInfos.Values)
                {
                    Debug.Log($"clients id {platformClientInfo.ClientID} name = {platformClientInfo.PlayerPresentationConfig.PlayerName}");
                }
            }
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
            Debug.Log("Sending player presentation config to server - " + LocalClientID);
            _commsHandler.SendMessage(playerPresentationConfig.Bytes, PlatformNetworkingMessageCodes.UpdatePlayerPresentation, TransmissionProtocol.TCP);
        }

        public void TearDown()
        {
            _commsHandler?.DisconnectFromServer();

            if (_playerSettingsHandler != null)
                _playerSettingsHandler.OnPlayerPresentationConfigChanged -= HandlePlayerPresentationConfigChanged;
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

