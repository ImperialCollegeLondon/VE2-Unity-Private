using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using ViRSE.Core.Shared;
using ViRSE.PluginRuntime;
using static PlatformSerializables;

namespace ViRSE.InstanceNetworking
{
    public class V_PlatformIntegration : MonoBehaviour, IPlatformService //will need some customer-facing interfacing 
    {
        //If isPlatform, these settings will be overriden by whatever the platform says
        [SerializeField] private ServerType _serverType = ServerType.Local;
        [SerializeField] private string _localServerIP = "127.0.0.1";
        [SerializeField] private string _remoteServerIP = "";
        [SerializeField] private ushort portNumber = 4296;

        [SerializeField] private bool UseSpoofUserIdentity = false;
        [SerializeField] UserIdentity spoofUserIdentity;

        private PlatformService _platformService;
        private PlatformService PlatformService  //We'll add platform components to the scene on start (e.g global info UI), this will let those components access the platform after domain reload
        {
            get  {
                if (_platformService == null)
                    Awake();

                return _platformService;
            }
            set {
                _platformService = value;
            }
        }

        #region Instance-Sync-Facing Interfaces
        public bool IsConnectedToServer => _platformService.IsConnectedToServer;
        public event Action OnConnectedToServer { add => _platformService.OnConnectedToServer += value; remove => _platformService.OnConnectedToServer -= value; }
        //Connect to server 
        #endregion

        //Instance allocation request is going to have to be in a private interface
        //No, NONE of this platform stuff is going to be accessible to the customer. 
        //True, they'll have the actual interface definition, but none of the concretes 
        //Right, but that means they'll be able to then search for that interface at runtime, and call it 
        //So we need an interface that's literally just "ConnectionDetailsProvider" then?

        /*   REQS
         *   We want the customer to be able to change the IP address for the INSTANCE syncer, and trigger the instance syncer connection
         *        -If we have that API, why not just have the platform do it that way?
         *        -Because it's not the platform's job, the instance-sync-facing platform API is ONLY meant for retrieving the connection details, and that's it
         *        -If the customer gets those details... then... it's fine? 
         *   
         *   The instance syncer should use the CDs from the platform, if present, otherwise use the customers
         *   
         *   We don't want the customer changing the instance sync CDs if on platform, or if the connection has been made already
         *   
         *   
         *   So surely we're fine 
         *   
         *   
         *   What we DO need, is an interface for instance allocation, getting the global info, etc. But that interface can be marked internal, so other namespaces can't see it!
         *   
         *   
         *   Anything that talks to the platform, should do it through this MonoBehaviour. They'll all have to find a reference to this Mono through FindObject, but that's fine, not too many objects needing to do that
         * 
         * 
         * 
         */


        private void Awake()
        {
            if (_platformService != null)
                return;

            Debug.Log("Platform integration mono awake!");

            if (_serverType != ServerType.Offline) //TODO, does offline even make sense here?
            {
                string ipAddressString;
                ipAddressString = _serverType == ServerType.Local ? _localServerIP : _remoteServerIP;

                if (IPAddress.TryParse(ipAddressString, out IPAddress ipAddress) == false)
                    throw new System.Exception("Invalid IP address, could not connect to server");

                _platformService = PlatformServiceFactory.Create(GetUserIdentity(), ipAddress, portNumber);
            }
        }

        private UserIdentity GetUserIdentity()
        {
            if (UseSpoofUserIdentity && Application.isEditor)
            {
                return spoofUserIdentity;
            }
            else
            {
                UserIdentity userIdentity = null;

                try
                {
                    string[] commandLine = System.Environment.GetCommandLineArgs();

                    if (commandLine.Length >= 7)
                    {
                        userIdentity = new(
                            commandLine[2],
                            commandLine[3],
                            commandLine[4],
                            commandLine[5],
                            commandLine[6]);
                    }
                }
                catch { }

                if (userIdentity == null)
                    userIdentity = new UserIdentity("test", "guest", "first", "last", "machine");

                return userIdentity;
            }
        }

        private void FixedUpdate()
        {
            //PlatformServuce.NetworkUpdate();
        }

        //TODO, API to change instance code, and to connect/disconnect
        //API for connect, disconnect, change instance code 

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                //Disable the player config settings on the spawner??
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {

            }
            else
            {
                _platformService.TearDown();
            }
        }

        public InstanceConnectionDetails GetInstanceConnectionDetails()
        {
            throw new System.NotImplementedException();
        }
    }
}


