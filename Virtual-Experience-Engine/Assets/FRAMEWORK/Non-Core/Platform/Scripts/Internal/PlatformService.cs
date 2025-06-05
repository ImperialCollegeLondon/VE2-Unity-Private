using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using UnityEngine.SceneManagement;
using System.Linq;
using static VE2.NonCore.Platform.Internal.PlatformSerializables;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;
using VE2.Core.Player.API;
using VE2.NonCore.Platform.API;
using static VE2.Core.Player.API.PlayerSerializables;
using VE2.Core.UI.API;
using VE2.Common.API;
using VE2.Common.Shared;

namespace VE2.NonCore.Platform.Internal
{
    internal static class PlatformServiceFactory
    {
        internal static PlatformService Create(IPlatformSettingsHandler platformSettingsHandler)
        {
            PlatformCommsHandler commsHandler = new(new DarkRift.Client.DarkRiftClient());
            IPlayerServiceInternal playerService = VE2API.Player as IPlayerServiceInternal; //TODO: Think about this - pretty sure the platform can just take the container, rather than initing the player directly 
            PluginLoader pluginLoader = new PluginLoader(platformSettingsHandler, playerService);
            return new PlatformService(
                commsHandler, 
                pluginLoader, 
                playerService, 
                platformSettingsHandler, 
                VE2API.PrimaryUIService as IPrimaryUIServiceInternal);
        }
    }

    internal class PlatformService: IPlatformServiceInternal
    {
        #region Interfaces //TODO - separate this between public and internal interfaces
        public ushort LocalClientID { get => _platformSettingsHandler.PlatformClientID; private set => _platformSettingsHandler.PlatformClientID = value; }
        public Dictionary<string, WorldDetails> ActiveWorlds { get => _platformSettingsHandler.ActiveWorlds; private set => _platformSettingsHandler.ActiveWorlds = value; }
        public string CurrentInstanceNumber => _platformSettingsHandler.InstanceCode.InstanceSuffix;
        public string CurrentWorldName => _platformSettingsHandler.InstanceCode.WorldName;

        public bool IsConnectedToServer { get; private set; }
        public event Action OnConnectedToServer;
        public bool IsAuthFailed { get; private set; }
        public event Action OnAuthFailed;
        public GlobalInfo GlobalInfo { get; private set; }

        public List<(string, int)> ActiveWorldsNamesAndVersions {
            get 
            {
                List<(string, int)> activeWorldsNamesAndVersions = new();

                foreach (WorldDetails worldDetails in ActiveWorlds.Values)
                    activeWorldsNamesAndVersions.Add((worldDetails.Name, worldDetails.VersionNumber));

                return activeWorldsNamesAndVersions;
            }
        }

        public event Action<GlobalInfo> OnGlobalInfoChanged;
        public event Action OnLeavingInstance;

        public void RequestInstanceAllocation(InstanceCode instanceCode)
        {
            if (IsConnectedToServer)
            {
                Debug.Log($"Requesting instance allocation to {instanceCode}");
                InstanceAllocationRequest instanceAllocationRequest = new(instanceCode);
                _commsHandler.SendMessage(instanceAllocationRequest.Bytes, PlatformNetworkingMessageCodes.InstanceAllocationRequest, TransmissionProtocol.TCP);
            }
            else
            {
                Debug.LogError("Not yet connected to server");
            }
        }

        /// <summary>
        /// Should happen once we arrive in the hub
        /// </summary>
        public void RequestHubAllocation()
        {
            RequestInstanceAllocation(new InstanceCode("Hub", "Solo", 0));
        }

        public void ReturnToHub()
        {
            _pluginLoader.LoadHub();
        }

        public ServerConnectionSettings GetInstanceServerSettingsForWorld(string worldName)
        {
            if (ActiveWorlds != null && ActiveWorlds.ContainsKey(worldName) && ActiveWorlds[worldName].HasCustomInstanceServer)
                return ActiveWorlds[worldName].CustomInstanceServerSettings;
            else
                return _platformSettingsHandler.FallbackInstanceServerSettings;
        }

        public ServerConnectionSettings GetInstanceServerSettingsForCurrentWorld() => GetInstanceServerSettingsForWorld(SceneManager.GetActiveScene().name); 

        private ServerConnectionSettings GetFTPSettingsForWorld(string worldName)
        {
            if (ActiveWorlds != null && ActiveWorlds.ContainsKey(worldName) && ActiveWorlds[worldName].HasCustomFTPServer)
                return ActiveWorlds[worldName].CustomFTPServerSettings;
            else
                return _platformSettingsHandler.FallbackWorldSubStoreFTPServerSettings;
        }

        public ServerConnectionSettings GetWorldSubStoreFTPSettingsForCurrentWorld() => GetFTPSettingsForWorld(SceneManager.GetActiveScene().name);

        public ServerConnectionSettings GetInternalWorldStoreFTPSettings() => _platformSettingsHandler.WorldBuildsFTPServerSettings;

        //Called by hub
        public void UpdateSettings(ServerConnectionSettings serverConnectionSettings, InstanceCode instanceCode)
        {
            Debug.Log("Update settings handler - " + serverConnectionSettings.ServerAddress + " - " + serverConnectionSettings.ServerPort + " - " + instanceCode);
            _platformSettingsHandler.PlatformServerConnectionSettings = serverConnectionSettings;
            _platformSettingsHandler.InstanceCode = instanceCode;
        }

        //Called by the hub, or by the provider, if we're in an plugin
        public void ConnectToPlatform()
        {
            Debug.Log("Connecting to platform with settings -" + _platformSettingsHandler.PlatformServerConnectionSettings.ServerAddress + "-:-" + _platformSettingsHandler.PlatformServerConnectionSettings.ServerPort + "-:-" + _platformSettingsHandler.InstanceCode);
            //CurrentInstanceCode = _platformSettingsHandler.InstanceCode;
            //IPAddress.Parse("PlatformIP"), PlatformPort, instanceCode
            _commsHandler.ConnectToServerAsync(IPAddress.Parse(_platformSettingsHandler.PlatformServerConnectionSettings.ServerAddress), _platformSettingsHandler.PlatformServerConnectionSettings.ServerPort);
        }

        internal event Action<InstanceCode> OnInstanceCodeChange;

        /*
                We should probably just be hiding UserSettingsDebug when there IS a platform service?
                Instead, maybe we can just show the platform's user settings directly into the debug thing?
                really, it's the instancing instance code we want to see here. 
                The platform doesn't know about the instancce service
                Yeah ok so the instance service should find the primary UI service 
        */
        public string PlayerDisplayName => _playerService.OverridableAvatarAppearance.PresentationConfig.PlayerName;

        public InstanceCode CurrentInstanceCode { get => _platformSettingsHandler.InstanceCode; private set => _platformSettingsHandler.InstanceCode = value; }

        public Dictionary<InstanceCode, PlatformInstanceInfo> InstanceInfos => GlobalInfo.InstanceInfos;
        public event Action<Dictionary<InstanceCode, PlatformInstanceInfo>> OnInstanceInfosChanged;
        #endregion

        private readonly IPlatformCommsHandler _commsHandler;
        private readonly PluginLoader _pluginLoader;
        private readonly IPlayerServiceInternal _playerService;
        private readonly IPlatformSettingsHandler _platformSettingsHandler;

        internal PlatformService(IPlatformCommsHandler commsHandler, PluginLoader pluginLoader, IPlayerServiceInternal playerService, 
            IPlatformSettingsHandler platformSettingsHandler, IPrimaryUIServiceInternal primaryUIService)
        {
            _commsHandler = commsHandler;
            _pluginLoader = pluginLoader;
            _playerService = playerService;
            _platformSettingsHandler = platformSettingsHandler;

            if (_playerService != null)
                _playerService.OnOverridableAvatarAppearanceChanged += HandlePlayerPresentationConfigChanged;

            commsHandler.OnReceiveNetcodeConfirmation += HandleReceiveNetcodeVersion;
            commsHandler.OnReceiveServerRegistrationConfirmation += HandleReceiveServerRegistrationResponse;
            commsHandler.OnReceiveGlobalInfoUpdate += HandleReceiveGlobalInfoUpdate;

            if (primaryUIService != null)
            {
                //Player browser=====
                GameObject playerBrowserUIHolder = GameObject.Instantiate(Resources.Load<GameObject>("PlatformPlayerBrowserUIHolder"));
                GameObject playerBrowserUI = playerBrowserUIHolder.transform.GetChild(0).gameObject;
                playerBrowserUI.SetActive(false);
            
                primaryUIService.AddNewTab("Players", playerBrowserUI, Resources.Load<Sprite>("PlatformPlayerBrowserUIICon"), 1);
                GameObject.Destroy(playerBrowserUIHolder);   

                //Quick panel=====
                GameObject platformQuickPanelUIHolder = GameObject.Instantiate(Resources.Load<GameObject>("PlatformQuickUIPanelHolder"));
                GameObject platformQuickPanelUI = platformQuickPanelUIHolder.transform.GetChild(0).gameObject;

                primaryUIService.SetPlatformQuickpanel(platformQuickPanelUI);
                GameObject.Destroy(platformQuickPanelUIHolder);
            }
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
                string customerID = "test", customerKey = "test"; //TODO - figure out these too!

                Debug.Log("Rec netcode - requesting reg into " + CurrentInstanceCode.ToString());
                ServerRegistrationRequest serverRegistrationRequest = new(customerID, customerKey, CurrentInstanceCode, _playerService.OverridableAvatarAppearance.PresentationConfig);
                _commsHandler.SendMessage(serverRegistrationRequest.Bytes, PlatformNetworkingMessageCodes.ServerRegistrationRequest, TransmissionProtocol.TCP);
            }
        }

        private void HandleReceiveServerRegistrationResponse(byte[] bytes)
        {
            ServerRegistrationResponse serverRegistrationConfirmation = new(bytes);

            //TODO: Check if auth success

            LocalClientID = serverRegistrationConfirmation.LocalClientID;
            GlobalInfo = serverRegistrationConfirmation.GlobalInfo;

            //TODO - maybe these should not come in from registation, perhaps should be requested by the hub specifically?
            ActiveWorlds = serverRegistrationConfirmation.ActiveWorlds;
            _platformSettingsHandler.WorldBuildsFTPServerSettings = serverRegistrationConfirmation.WorldBuildsFTPServerSettings;
            _platformSettingsHandler.FallbackInstanceServerSettings = serverRegistrationConfirmation.DefaultInstanceServerSettings;
            _platformSettingsHandler.FallbackWorldSubStoreFTPServerSettings = serverRegistrationConfirmation.DefaultWorldSubStoreFTPServerSettings;

            Debug.Log("Server says our fallback instance server IP is " + serverRegistrationConfirmation.DefaultInstanceServerSettings.ServerAddress);

            IsConnectedToServer = true;

            Debug.Log("Receive reg response from server " + LocalClientID);

            OnConnectedToServer?.Invoke();
            OnGlobalInfoChanged?.Invoke(GlobalInfo);
        }

        private void HandleReceiveGlobalInfoUpdate(byte[] bytes)
        {
            GlobalInfo newGlobalInfo = new(bytes);


            if (GlobalInfo != null && newGlobalInfo.Bytes.SequenceEqual(GlobalInfo.Bytes))
                return;

            //TODO - might be better to have the instance allocation be a specific message, rather than being ended implicitely in the global info update?
            //Are we then worried about missing the message?
            PlatformInstanceInfo newLocalInstanceInfo = newGlobalInfo.InstanceInfoForClient(LocalClientID);

            if (newLocalInstanceInfo == null)
            {
                Debug.LogError($"No instance info found for local client (#{LocalClientID}) in global info update. Printing all instance infos:");
                foreach (var instanceInfo in newGlobalInfo.InstanceInfos)
                {
                    Debug.Log($"Instance Code: {instanceInfo.Key} ===========");
                    foreach (var clientInfo in instanceInfo.Value.ClientInfos)
                    {
                        Debug.Log($"  Client ID: {clientInfo.Key}");
                    }
                }
                return;
            }

            if (CurrentInstanceCode == null || !newLocalInstanceInfo.InstanceCode.Equals(CurrentInstanceCode))
            {
                Debug.Log($"INSTANCE ALLOC, CURRENT CODE NULL? {CurrentInstanceCode == null}, new code {newLocalInstanceInfo.InstanceCode}");
                Debug.Log($"old code {CurrentInstanceCode}");
                HandleInstanceAllocation(newLocalInstanceInfo);
            }

            GlobalInfo = newGlobalInfo;

            //TODO - tidy these up, both doing the same thing!
            OnGlobalInfoChanged?.Invoke(GlobalInfo);
            OnInstanceInfosChanged?.Invoke(GlobalInfo.InstanceInfos);
        }

        private void HandleInstanceAllocation(PlatformInstanceInfo newInstanceInfo)
        {
            Debug.Log($"<color=green>Detected allocation to new instance, going to {newInstanceInfo.InstanceCode.ToString()}</color>");

            CurrentInstanceCode = newInstanceInfo.InstanceCode;

            //TODO - these two are a bit redundant, rework!
            OnLeavingInstance?.Invoke();
            OnInstanceCodeChange?.Invoke(CurrentInstanceCode);

            if (newInstanceInfo.InstanceCode.WorldName.ToUpper().Equals("HUB"))
            {
                SceneManager.LoadScene("Hub");
            }
            else
            {
                _pluginLoader.LoadPlugin(newInstanceInfo.InstanceCode.WorldName, newInstanceInfo.InstanceCode.VersionNumber);
            }
        }

        internal void MainThreadUpdate()
        {
            _commsHandler.MainThreadUpdate();
        }

        internal void NetworkUpdate()
        {
            //if (ConnectedToServer)
            //{

            //}
        }

        private void ReceivePingFromHost()
        {
            //TODO calc buffer size
        }

        private void HandlePlayerPresentationConfigChanged(OverridableAvatarAppearance overridableAvatarAppearance)
        {
            Debug.Log("Sending player presentation config to server - " + LocalClientID);
            if (IsConnectedToServer)
                _commsHandler.SendMessage(overridableAvatarAppearance.PresentationConfig.Bytes, PlatformNetworkingMessageCodes.UpdatePlayerPresentation, TransmissionProtocol.TCP);
        }

        public void TearDown()
        {
            _commsHandler?.DisconnectFromServer();

            if (_playerService != null)
                _playerService.OnOverridableAvatarAppearanceChanged -= HandlePlayerPresentationConfigChanged;
        }

        // void IPlatformServiceInternal.RequestInstanceAllocation(string worldFolderName, string instanceSuffix, string versionNumber)
        // {
        //     RequestInstanceAllocation(worldFolderName, instanceSuffix, versionNumber);
        // }
    }

    internal static class GlobalInfoExtensions
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

