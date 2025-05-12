using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using VE2.NonCore.Instancing.API;
using VE2.NonCore.Instancing.Internal;
using VE2.Core.VComponents.API;
using VE2.Core.Common;

namespace VE2.NonCore.Instancing.VComponents.Tests
{
    [TestFixture]
    [Category("Network Object Tests")]
    internal class NetworkObjectTests
    {

        [Test]
        public void NetworkObject_WhenObjectIsSet_EmitsToPlugin()
        {
            //Arrange=========
            //  Create the NetworkObjectService, injecting default configs 
            NetworkObjectService networkObjectService = new( new NetworkObjectStateConfig(), new NetworkObjectState(), "test", Substitute.For<IWorldStateSyncService>());
            //  Create a stub for the VC
            V_NetworkObjectStub v_networkObjectStub = new(networkObjectService);
            //  Get the plugin-facing interface out of the VC 
            IV_NetworkObject networkObjectInterface = v_networkObjectStub;
            //  Create a substitute for the PluginScript, wire it up to the plugin interface 
            PluginScript customerScript = new();
            networkObjectInterface.OnStateChange.AddListener(customerScript.HandleObjectReceived);

            //Act=========
            //  Programmatically set the network object 
            System.Random random = new();
            int serializableObject = random.Next(int.MinValue, int.MaxValue);
            networkObjectInterface.NetworkObject = serializableObject;

            // ===========Assert=========
            // Check the customer received the same object
            Assert.IsInstanceOf<int>(customerScript.ReceivedObject);
            Assert.AreEqual((int)customerScript.ReceivedObject, serializableObject);

            // Check also that HandleObjectReceived was only called once
            Assert.AreEqual(customerScript.ReceivedCounter, 1);

            //Later on, we'll think about how to do an intergration test, where we get the syncer to set the value on the network object 
        }
    }

    internal class PluginScript
    {
        public object ReceivedObject;
        public int ReceivedCounter = 0;

        public virtual void HandleObjectReceived(object obj)
        {
            ReceivedObject = obj;
            ReceivedCounter++;
        }
    }

    internal class V_NetworkObjectStub : IV_NetworkObject //We can't Substitute.For a MonoBehaviour, so we create an explicit test double class instead 
    {
        #region Plugin Interfaces
        INetworkObjectStateModule IV_NetworkObject._StateModule => _NetworkService.StateModule;
        #endregion

        protected NetworkObjectService _NetworkService = null;

        public V_NetworkObjectStub(NetworkObjectService networkService)
        {
            _NetworkService = networkService;
        }
    }
}
