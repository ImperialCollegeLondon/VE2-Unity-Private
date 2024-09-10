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
namespace ViRSE.PluginRuntime
{
    public static class PlatformServiceFactory
    {
        public static PlatformService Create(UserIdentity userIdentity, IPAddress ipAddress, ushort portNumber)
        {
            PlatformCommsHandler commsHandler = new(new DarkRift.Client.DarkRiftClient());
            return new PlatformService(commsHandler, userIdentity, ipAddress, portNumber);
        }
    }

    /*
     *   This needs to persist between scenes, but, since we need it to function after domain reload, we also need it to function in the scene with the syncer immediately, and across multiple syncers 
     *   We need some mono to detect when we enter a new scene. PlatformService then needs to forward to IP/port to the instance sync service 
     *   mmmm, maybe this aint actually right 
     *   "PlatformService" should maybe just be used for fetching data from the platform, and making requests to the platform 
     *   It should probably be some other component that is in charge of orchestrating the instance stuff and the platform stuff
     *   
     *   Tbf... maybe the instance sync service should be responsible for fetching the IP and port from the platform service?
     *   Ok, so how would this work
     *   InstanceSync service starts, when it starts, it gets passed a reference to the PlatformService, which then let's us get the IP/port
     *   If there IS no platform service, then the scene syncer uses its regular IP and port 
     *   If there is a platform service that hasn't yet been initialized, we do a lazy operation. We wait for the platform service to be initialized, and then we get the IP/port once we get some "ConnectedToServer" event
     * 
     * 
     */

    //TODO, we might want to consider cutting down the duplicate code with the instance sync stuff
    public class PlatformService : IPlatformService
    {
        public bool IsConnectedToServer { get; private set; }
        public event Action OnConnectedToServer;

        private UserIdentity _userIdentity;

        private string _instanceCode;
        private ushort _localClientID;
        private GlobalInfo _GlobalInfo;
        private Dictionary<string, WorldDetails> _availableWorlds;

        private IPlatformCommsHandler _commsHandler;


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

            /*
             *   So when we connect to the server for the first time, it gives us a bunch of worlds 
             *   We then request that world, and we go there. 
             *   These worlds all come from a DB
             *   But what about when the platform is running locally?
             *   Also, how do we update the DB while it is running?
             *   Yeah
             *   
             *   Ok, so I guess we just read in some text file? And use that as worlds?
             */
        }
        #endregion

        public PlatformService(IPlatformCommsHandler commsHandler, UserIdentity userIdentity, IPAddress ipAddress, ushort portNumber)
        {
            _userIdentity = userIdentity;
            _commsHandler = commsHandler;

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
                Debug.Log("Rec platform nv, sending reg");

                ServerRegistrationRequest serverRegistrationRequest = new(_userIdentity);
                _commsHandler.SendServerRegistrationRequest(serverRegistrationRequest.Bytes);
            }
        }

        private void HandleReceiveServerRegistrationConfirmation(byte[] bytes)
        {
            Debug.Log("Rec platform reg conf");
            ServerRegistrationConfirmation serverRegistrationConfirmation = new(bytes);
            Debug.Log("1");

            _localClientID = serverRegistrationConfirmation.LocalClientID;
            _GlobalInfo = serverRegistrationConfirmation.GlobalInfo;
            _availableWorlds = serverRegistrationConfirmation.AvailableWorlds;

            IsConnectedToServer = true;

            Debug.Log("2");

            //TODO try catch
            OnConnectedToServer?.Invoke(); //TODO, this is crapping out

            Debug.Log("Local client platform ID = " + _localClientID);
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

        public void TearDown()
        {
            if (IsConnectedToServer)
            {
                _commsHandler.DisconnectFromServer();
            }
            //Probably destroy remote players?
        }

        public InstanceConnectionDetails GetInstanceConnectionDetails(string worldName)
        {
            throw new NotImplementedException();
        }

        public InstanceConnectionDetails GetInstanceConnectionDetails()
        {
            throw new NotImplementedException();
        }
    }
}
