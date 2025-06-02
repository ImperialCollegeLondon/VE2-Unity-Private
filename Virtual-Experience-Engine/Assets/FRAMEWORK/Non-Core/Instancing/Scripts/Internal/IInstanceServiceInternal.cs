using System;
using UnityEngine;
using VE2.NonCore.Instancing.API;

namespace VE2.NonCore.Instancing.Internal
{
    internal interface IInstanceServiceInternal : IInstanceService
    {
        public void SendInstantMessage(string id, object message);

        public void RegisterInstantMessageHandler(string id, IInstantMessageHandlerInternal instantMessageHandler);
        public void DeregisterInstantMessageHandler(string id);
    }
}
