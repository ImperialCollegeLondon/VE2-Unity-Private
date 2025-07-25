using System;
using UnityEngine;
using static VE2.NonCore.Instancing.API.InstancePublicSerializables;

namespace VE2.NonCore.Instancing.API
{
    internal interface IInstanceServiceInternal : IInstanceService
    {
        public void SendInstantMessage(string id, object message);

        public void RegisterInstantMessageHandler(string id, IInstantMessageHandlerInternal instantMessageHandler);
        public void DeregisterInstantMessageHandler(string id);

        public event Action OnBecomeHostInternal;
        public event Action OnBecomeNonHostInternal;

        public InstancedInstanceInfo InstanceInfo { get; }

        public event Action<InstancedInstanceInfo> OnInstanceInfoChanged;
    }
}
