using UnityEngine;
using UnityEngine.Events;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;

namespace VE2.NonCore.Instancing.Internal
{
    internal class V_InstantMessageHandler : MonoBehaviour, IV_InstantMessageHandler, IInstantMessageHandler
    {

        [SerializeField, HideLabel, IgnoreParent] private InstantMessageHandlerConfig _config = new();
        private InstantMessageHandlerService _service;

        public void SendInstantMessage(object message) => _service.SendInstantMessage(message);

        public UnityEvent<object> OnMessageReceived() => _config.OnMessageReceived;

        private void OnEnable()
        {
            string id = "IMH-" + gameObject.name;
            _service = new InstantMessageHandlerService(_config, id, (IInstanceServiceInternal)InstancingAPI.InstanceService);
        }

        public void ReceiveInstantMessage(object message)
        {
            _service.ReceiveInstantMessage(message);
        }
    }
}
