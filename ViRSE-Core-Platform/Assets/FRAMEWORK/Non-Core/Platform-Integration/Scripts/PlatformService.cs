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
using static CommonNetworkObjects;
using static PlatformNetworkObjects;
using static DarkRift.Server.DarkRiftInfo;
using ViRSE.InstanceNetworking;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace ViRSE.PluginRuntime
{
    public static class PlatformServiceFactory
    {
        public static PlatformService Create(UserIdentity userIdentity)
        {
            PlatformCommsHandler commsHandler = new(new DarkRift.Client.DarkRiftClient());
            return new PlatformService(commsHandler, userIdentity);
        }
    }

    public class PlatformService //TODO, we might want to consider cutting down the duplicate code with the instance sync stuff
    {
        private bool _readyToSync = false;

        private UserIdentity _userIdentity;

        private string _instanceCode;
        private ushort _localClientID;
        private GlobalInfo _GlobalInfo;
        private Dictionary<string, WorldDetails> _availableWorlds;

        private IPlatformCommsHandler _commsHandler;

        public PlatformService(IPlatformCommsHandler commsHandler, UserIdentity userIdentity)
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
        }

        public void ConnectToServer(IPAddress ipAddress, int portNumber, string instanceCode)
        {
            _instanceCode = instanceCode;
            _commsHandler.ConnectToServer(ipAddress, portNumber);
        }

        private void HandleReceiveNetcodeVersion(byte[] bytes)
        {
            NetcodeVersionConfirmation netcodeVersionConfirmation = new(bytes);

            if (netcodeVersionConfirmation.NetcodeVersion != PlatformNetworkObjects.PlatformNetcodeVersion)
            {
                //TODO - handle bad netcode version
                Debug.LogError($"Bad platform netcode version, received version {netcodeVersionConfirmation.NetcodeVersion} but we are on {PlatformNetworkObjects.PlatformNetcodeVersion}");
            } 
            else
            {
                Debug.Log("Rec platform nv, sending reg");

                ServerRegistrationRequest serverRegistrationRequest = new(_userIdentity, _instanceCode);
                _commsHandler.SendServerRegistrationRequest(serverRegistrationRequest.Bytes);
            }
        }

        private void HandleReceiveServerRegistrationConfirmation(byte[] bytes)
        {
            ServerRegistrationConfirmation serverRegistrationConfirmation = new(bytes);

            Debug.Log("Rec platform reg conf");

            _localClientID = serverRegistrationConfirmation.LocalClientID;
            _GlobalInfo = serverRegistrationConfirmation.GlobalInfo;
            _availableWorlds = serverRegistrationConfirmation.AvailableWorlds;

            _readyToSync = true;

            V_SceneSyncer sceneSyncer = GameObject.FindObjectOfType<V_SceneSyncer>();
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
        }

        private void HandleReceiveGlobalInfoUpdate(byte[] bytes)
        {
            GlobalInfo newGlobalInfo = new(bytes);

            //Handle all the things! Mainly, instance allocations
        }

        public void NetworkUpdate()
        {
            if (_readyToSync)
            {

            }
        }

        public void ReceivePingFromHost()
        {
            //TODO calc buffer size
        }

        public void TearDown()
        {
            //Probably destroy remote players?
        }
    }
}
