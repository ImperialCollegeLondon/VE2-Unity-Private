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
using static DarkRift.Server.DarkRiftInfo;
using ViRSE.InstanceNetworking;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

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

    //TODO, we might want to consider cutting down the duplicate code with the instance sync stuff
    public class PlatformService : IPlatformService
    {
        private bool _connectedToServer = false;

        private UserIdentity _userIdentity;

        private string _instanceCode;
        private ushort _localClientID;
        private GlobalInfo _GlobalInfo;
        private Dictionary<string, WorldDetails> _availableWorlds;

        private IPlatformCommsHandler _commsHandler;

        #region Platform-Private Interfaces
        public void RequestInstanceAllocation(string worldName, string instanceSuffix)
        {
            if (_connectedToServer)
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

            V_SceneSyncer sceneSyncer = GameObject.FindObjectOfType<V_SceneSyncer>();
            if (sceneSyncer != null)
            {
                //Disable the scene syncer until we have the IP address to point it towards
                sceneSyncer.ConnectAutomatically = false;
            }

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

            _connectedToServer = true;

            Debug.Log("2..");

            V_SceneSyncer sceneSyncer = null;
            Debug.Log("3");
            sceneSyncer = GameObject.FindObjectOfType<V_SceneSyncer>();
            Debug.Log("4");

            if (sceneSyncer != null)
            {
                if (_availableWorlds.TryGetValue(SceneManager.GetActiveScene().name, out WorldDetails worldDetails))
                {
                    sceneSyncer.ConnectToServer(worldDetails.IPAddress, worldDetails.PortNumber, _instanceCode);
                }
                else
                {
                    Debug.LogError("Could not find world details for current scene, connecting to local instance relay");
                    sceneSyncer.ConnectToServer(worldDetails.IPAddress, worldDetails.PortNumber, _instanceCode);
                }
            }

            Debug.Log("Local client platform ID = " + _localClientID);
        }

        private void HandleReceiveGlobalInfoUpdate(byte[] bytes)
        {
            GlobalInfo newGlobalInfo = new(bytes);

            //Handle all the things! Mainly, instance allocations
        }

        public void NetworkUpdate()
        {
            if (_connectedToServer)
            {

            }
        }

        public void ReceivePingFromHost()
        {
            //TODO calc buffer size
        }

        public void TearDown()
        {
            if (_connectedToServer)
            {
                _commsHandler.DisconnectFromServer();
            }
            //Probably destroy remote players?
        }
    }
}
