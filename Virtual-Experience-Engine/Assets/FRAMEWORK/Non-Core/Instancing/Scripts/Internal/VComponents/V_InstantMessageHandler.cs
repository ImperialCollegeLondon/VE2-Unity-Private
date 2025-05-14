using UnityEngine;
using UnityEngine.Events;
using VE2.Common.API;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;

namespace VE2.NonCore.Instancing.Internal
{
    internal class V_InstantMessageHandler : MonoBehaviour, IV_InstantMessageHandler
    {

        [SerializeField, HideLabel, IgnoreParent] private InstantMessageHandlerConfig _config = new();
        private InstantMessageHandlerService _service;
        private IInstanceServiceInternal _internalInstanceService;
        private string _id;

        public void SendInstantMessage(object message) => _service.SendInstantMessage(message);
        
        public UnityEvent<object> OnMessageReceived => _config.OnMessageReceived;

        private void OnEnable()
        {
            _id = "IMH-" + gameObject.name;

            if (VE2API.InstanceService == null)
            {
                Debug.LogError("Instance service is null, cannot initialise RigidbodySyncable, please add a V_InstanceIntegration component to the scene.");
                return;
}

            _service = new InstantMessageHandlerService(_config, _id, VE2API.InstanceService as IInstanceServiceInternal);
        }

        private void OnDisable()
        {
            _service?.TearDown();
            _service = null;
        }

    }
}
