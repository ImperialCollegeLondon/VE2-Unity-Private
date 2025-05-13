using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using VE2.NonCore.Instancing.API;
using VE2.NonCore.Instancing.Internal;
using VE2.Core.VComponents.API;
using VE2.Core.Common;
using UnityEngine.Events;

namespace VE2.NonCore.Instancing.VComponents.Tests
{
    [TestFixture]
    [Category("Instant Message Handler Tests")]
    internal class InstantMessageHandlerTests
    {

        [Test]
        public void InstantMessageHandler_WhenMessageIsSent_OnMessageReceivedCalled()
        {
            //Arrange=========
            //  Create the InstantMessageHandlerService, injecting default configs
            InstantMessageHandlerConfig config = new();
            InstantMessageHandlerService service = new( config, "test", Substitute.For<IInstanceServiceInternal>());
            //  Create a stub for the VC
            V_InstantMessageHandlerStub v_instantMessageHandlerStub = new(service, config);
            //  Get the plugin-facing interface out of the VC 
            IV_InstantMessageHandler instantMessageHandlerInterface = v_instantMessageHandlerStub;
            //  Create a substitute for the PluginScript, wire it up to the plugin interface 
            InstantMessageHandlerTestPluginScript customerScript = new();
            instantMessageHandlerInterface.OnMessageReceived.AddListener(customerScript.HandleMessageReceived);

            //Act=========
            //  Programmatically send an instant message
            System.Random random = new();
            int serializableObject = random.Next(int.MinValue, int.MaxValue);
            instantMessageHandlerInterface.SendInstantMessage(serializableObject);

            // ===========Assert=========
            // Check the customer received the same message
            Assert.IsInstanceOf<int>(customerScript.ReceivedMessage);
            Assert.AreEqual((int)customerScript.ReceivedMessage, serializableObject);

            // Check also that HandleMessageReceived was only called once
            Assert.AreEqual(customerScript.ReceivedCounter, 1);

        }
    }

    internal class InstantMessageHandlerTestPluginScript
    {
        public object ReceivedMessage;
        public int ReceivedCounter = 0;

        public virtual void HandleMessageReceived(object obj)
        {
            ReceivedMessage = obj;
            ReceivedCounter++;
        }
    }

    internal class V_InstantMessageHandlerStub : IV_InstantMessageHandler //We can't Substitute.For a MonoBehaviour, so we create an explicit test double class instead 
    {
        
        private InstantMessageHandlerService _service = null;
        private InstantMessageHandlerConfig _config = null;

        public V_InstantMessageHandlerStub(InstantMessageHandlerService instantMessageHandlerService, InstantMessageHandlerConfig config)
        {
            _service = instantMessageHandlerService;
            _config = config;
        }

        public UnityEvent<object> OnMessageReceived => _config.OnMessageReceived;

        public void SendInstantMessage(object message) => _service.SendInstantMessage(message);
    }
}
