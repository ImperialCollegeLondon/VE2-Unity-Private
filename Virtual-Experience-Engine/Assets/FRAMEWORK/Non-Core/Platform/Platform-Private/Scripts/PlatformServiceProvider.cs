using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VE2.NonCore.Platform.Private;
using static VE2.PlatformNetworking.PlatformSerializables;

namespace VE2.PlatformNetworking
{
    public class PlatformServiceProvider : MonoBehaviour//, IPlatformServiceProvider
    {
        //If isPlatform, these settings will be overriden by whatever the platform says
        [SerializeField] private ServerType _serverType = ServerType.Local;
        [SerializeField] private string _localServerIP = "127.0.0.1";
        [SerializeField] private string _remoteServerIP = "";
        [SerializeField] private ushort _portNumber = 4298;

        [Space(10)]
        [SerializeField, HideInInspector] private string _instanceCode = null;

        [Space(10)]
        [SerializeField] private bool UseSpoofUserIdentity = false;
        //[SerializeField] UserIdentity spoofUserIdentity;

    }
}

