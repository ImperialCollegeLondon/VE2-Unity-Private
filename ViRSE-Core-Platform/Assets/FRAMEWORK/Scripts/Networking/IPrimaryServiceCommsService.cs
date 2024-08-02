using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ViRSE
{
    public interface IPrimaryServiceCommsService
    {
        public void RegisterWithServer(ServerType serverType);

        public UnityEvent<ServerRegistration> OnRegisterWithServer { get; }
        public UnityEvent OnDisconnectedFromServer { get; }
        public UnityEvent<Version> OnNetcodeVersionMismatch { get; }
    }
}
