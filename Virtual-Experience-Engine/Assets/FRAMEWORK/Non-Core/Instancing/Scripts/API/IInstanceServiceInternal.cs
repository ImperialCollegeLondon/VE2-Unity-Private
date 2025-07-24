using System;
using UnityEngine;

namespace VE2.NonCore.Instancing.API
{
    internal interface IInstanceServiceInternal : IInstanceService
    {
        public void SendInstantMessage(string id, object message);

        public void RegisterInstantMessageHandler(string id, IInstantMessageHandlerInternal instantMessageHandler);
        public void DeregisterInstantMessageHandler(string id);

        public event Action OnBecomeHostInternal;
        public event Action OnBecomeNonHostInternal;
    }
}
