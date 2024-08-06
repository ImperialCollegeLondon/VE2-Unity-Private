using Sirenix.OdinInspector;
using System;
using UnityEditor;
using UnityEngine;
using ViRSE;

namespace ViRSE.FrameworkRuntime
{
    public interface IPrimaryServerService //PluginService-facing interface, the server will get data some other way
    {
        //public bool RegisteredWithServer { get; }
        //public event Action OnServerHandshakeComplete;

        public ClientInfo LocalClientInfo { get; }
        public event Action OnLocalClientInfoUpdate;

        public InstanceInfo LocalInstanceInfo { get; }
        public event Action OnLocalInstanceInfoUpdate;

        public IPluginSyncCommsHandler PluginSyncCommsHandler { get; }
    }

    public class PrimaryServerService : MonoBehaviour, IPrimaryServerService
    {
        #region PluginService Interfaces
        //public bool RegisteredWithServer { get; private set; } = false;
        //public event Action OnServerHandshakeComplete;

        public ClientInfo LocalClientInfo { get; }
        public event Action OnLocalClientInfoUpdate;

        public InstanceInfo LocalInstanceInfo { get; }
        public event Action OnLocalInstanceInfoUpdate;

        public IPluginSyncCommsHandler PluginSyncCommsHandler => (IPluginSyncCommsHandler)_commsService;
        #endregion

        [SerializeField] private bool _spoofUserIdentity = false;

        [ShowIf("@_spoofUserIdentity")]
        [SerializeField] private UserIdentity _userIdentitySpoof;

        public UserIdentity UserIdentity { get; private set; }
        public UserSettings PlayerSettings { get; private set; }

        public event Action<UserSettings> OnPlayerSettingsReady;

        private PopulationInfoWrapper _populationInfo;

        #region Plugin Runtime Interfaces
        private IPrimaryServerCommsService _commsService;
        #endregion

        public void Initialize(ServerType serverType)
        {
            _commsService = gameObject.AddComponent<DarkRiftCommsService>();

            _commsService.OnReceiveNetcodeConfirmation += HandleReceiveNetcodeConfirmation;
            _commsService.OnReceiveServerRegistrationConfirmation += HandleReceiveServerRegistrationConfirmation;
            _commsService.OnReceivePopulationUpdate += HandlePopulationInfoUpdate;

            _commsService.ConnectToServer(serverType);
        }

        private void HandleReceiveNetcodeConfirmation(byte[] bytes)
        {
            NetcodeVersionConfirmation netcodeVersionConfirmation = new(bytes);

            string startingInstance = "Dev-Testing";  //TODO
            ServerRegistrationRequest serverRegistrationRequest = CreateServerRegistrationRequest(startingInstance);
            _commsService.SendServerRegistrationRequest(serverRegistrationRequest);
        }

        private void HandleReceiveServerRegistrationConfirmation(byte[] bytes)
        {
            ServerRegistrationConfirmation serverRegistrationConfirmation = new(bytes);
            _onPlayerSettingsReady?.Invoke(serverRegistrationConfirmation.UserSettings);
        }

        public void HandlePopulationInfoUpdate(byte[] populationAsBytes)
        {
            PopulationInfo populationInfo = new(populationAsBytes);
            
        }

        public void HandleServerHandshakeComplete(UserSettings playerSettings) //TODO, take in PlayerSettings
        {
            _onPlayerSettingsReady?.Invoke(playerSettings);
        }

        //TODO, refactor omds
        private ServerRegistrationRequest CreateServerRegistrationRequest(string startingInstance)
        {
            string[] commandLine = System.Environment.GetCommandLineArgs();
            UserIdentity userIdentity;
            string machineName;

            if (Application.isEditor && _spoofUserIdentity)
            {
                userIdentity = null;
                machineName = "???";
            }
            else if (Application.isEditor || commandLine.Length < 1)
            {
                userIdentity = null;
                machineName = "???";
            }
            else
            {
                try
                {
                    if (commandLine.Length > 0)
                    {
                        V_Logger.Dev("GOT CMD ARGS!");
                        foreach (string line in commandLine)
                            V_Logger.Dev("CMD ARG " + line);

                        if (commandLine.Length > 2 && !Application.isEditor)
                        {
                            userIdentity = new(
                                commandLine[2],
                                commandLine[3],
                                commandLine[4],
                                commandLine[5],
                                commandLine[6],
                                commandLine[7],
                                commandLine[8]);

                            machineName = commandLine[9]; //TODO, confirm this
                        }
                        else
                        {
                            userIdentity = null;
                            machineName = "???";
                        }
                    }
                    else
                    {
                        userIdentity = null;
                        machineName = "???";
                    }
                }
                catch
                {
                    V_Logger.Error("Couldn't create UserIdentity from cmd args, creating guest identity");
                    userIdentity = null;
                    machineName = "???";
                };
            }

            return new ServerRegistrationRequest(startingInstance, machineName, userIdentity);
        }

        private string GetMachineName()
        {
            string machineName = System.Environment.MachineName;

            string machineNameTrimmed = "???";
            if (machineName.StartsWith("SKRS-"))
                machineNameTrimmed = machineName.Substring(5);

            return machineNameTrimmed;
        }

        //Stuff other components need to know about 
        //When we connect 
        //When we disconnect
        //When we decide to route to a new instance 
        //When the netcode version doesn't match


        //Stuff the customer needs to know about 
        //Host status changes
        //Admin status changes 
        //When people join or leave the instance 


        /* How should this actually work
         * PrimaryServerService contains a reference to the DarkRift layer, we'll call this CommsHandler?
         * Comms handler should emit events back up to the PrimaryServerService when it needs to, we wont need this for all the messages though
         * Basically, we can abstract away the actual details of the network here, we can just say "CommsHandler.Register with server"...
         * ...And the commshandler can then deal with the flow, IE 
         */


        /* 
         * Currently, we just send a big fat list of client infos, 
         * We could send actual messages for "ClientJoin and ClientLeave"
         * Actually, let's keep it as it is, means we can drop connection and come back and continue as normal
         */
    }

    //TODO move to a different file

    //TODO - Default user settings should DEF come from the buiild 
    //Or should they, cz the database needs to have default settings anyway
    //I'd prefer to actually know if the build is receiving DB defaults, because it shouldn't 
    //In fact, if we're guest, or a new user, we shouldn't even have settings anyway
    //
}
