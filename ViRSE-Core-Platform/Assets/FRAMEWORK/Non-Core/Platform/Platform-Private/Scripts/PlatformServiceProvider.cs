using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ViRSE.NonCore.Platform.Private;
using static ViRSE.PlatformNetworking.PlatformSerializables;

namespace ViRSE.PlatformNetworking
{
    public class PlatformServiceProvider : MonoBehaviour, IPlatformServiceProvider
    {
        //If isPlatform, these settings will be overriden by whatever the platform says
        [SerializeField] private ServerType _serverType = ServerType.Local;
        [SerializeField] private string _localServerIP = "127.0.0.1";
        [SerializeField] private string _remoteServerIP = "";
        [SerializeField] private ushort _portNumber = 4296;

        [Space(10)]
        [SerializeField, HideInInspector] private string _instanceCode = null;

        [Space(10)]
        [SerializeField] private bool UseSpoofUserIdentity = false;
        [SerializeField] UserIdentity spoofUserIdentity;

        private PlatformService _platformService;
        public IPlatformService PlatformService  //We'll add platform components to the scene on start (e.g global info UI), this will let those components access the platform after domain reload
        {
            get {
                if (_platformService == null)
                    OnEnable();

                return _platformService;
            }
        }

        private void OnEnable()
        {
            if (_platformService != null || !Application.isPlaying)
                return;

            transform.parent = null;
            DontDestroyOnLoad(gameObject);

            //Debug.Log("Platform integration mono awake!");

            bool firstLoad = string.IsNullOrEmpty(_instanceCode);
            if (firstLoad) //Otherwise, the instance code will be carried over from its serialized state pre domain reload
            {
                if (FindObjectsByType<PlatformServiceProvider>(FindObjectsSortMode.None).Length > 1)
                {
                    //When returning to the hub, there may be two of these in the scene. If this is doing its first load, then this isn't the one we want to keep!
                    //On domain reload, there should only be one in the scene
                    Destroy(gameObject);
                    return;
                }

                string sceneName = SceneManager.GetActiveScene().name;

                if (sceneName.ToUpper().Equals("HUB"))
                {
                    _instanceCode = PlatformInstanceInfo.GetInstanceCode(sceneName, "Solo");
                }
                else
                {
                    //TODO - read command line args. This is the flow case where we've gone straight into a scene from the launcher, rather than going through the hub
                    throw new NotImplementedException();
                    //_instanceCode = PlatformInstanceInfo.GetInstanceCode(sceneName, "Solo");
                }
            }

            if (_serverType != ServerType.Offline) //TODO, does offline even make sense here?
            {
                string ipAddressString;
                ipAddressString = _serverType == ServerType.Local ? _localServerIP : _remoteServerIP;

                if (IPAddress.TryParse(ipAddressString, out IPAddress ipAddress) == false)
                    throw new System.Exception("Invalid IP address, could not connect to server");

                _platformService = (PlatformService)PlatformServiceFactory.Create(GetUserIdentity(), ipAddress, _portNumber, _instanceCode);
                _platformService.OnInstanceCodeChange += (newInstanceCode) => _instanceCode = newInstanceCode;  //To preserve our instance code should the domain reload
            }
        }

        private void Update()
        {
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                _platformService.RequestHubAllocation();
            }
        }

        private void FixedUpdate()
        {
            _platformService?.MainThreadUpdate();
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

                if (!Application.isEditor)
                {
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
                }

                if (userIdentity == null)
                    userIdentity = new UserIdentity("test", "guest", "first", "last", "machine");

                Debug.Log($"Generated User identity: {userIdentity.ToString()}");

                return userIdentity;
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
                _platformService?.TearDown();
        }
    }
}

