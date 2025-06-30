using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using VE2.NonCore.Instancing.API;
using VE2.NonCore.Instancing.Internal;
using VE2.Common.Shared;
using UnityEngine.Events;

namespace VE2.NonCore.Instancing.VComponents.Tests
{
    [TestFixture]
    [Category("Network Object Tests")]
    internal class NetworkObjectTests
    {

        [TestCase("Hello")]
        [TestCase(42)]
        [TestCase(3.14)]
        [TestCase(true)]
        public void NetworkObject_WhenObjectIsSet_EmitsToPlugin(object outgoingObject)
        {
            //Arrange=========
            //  Create the NetworkObjectService, injecting default configs 
            NetworkObjectService networkObjectService = new( new NetworkObjectStateConfig(), new NetworkObjectState(), "test", Substitute.For<IWorldStateSyncableContainer>());
            //  Create a stub for the VC
            V_NetworkObjectStub v_networkObjectStub = new(networkObjectService);
            //  Get the plugin-facing interface out of the VC 
            IV_NetworkObject networkObjectInterface = v_networkObjectStub;
            //  Create a substitute for the PluginScript, wire it up to the plugin interface 
            NetworkObjectTestPluginScript customerScript = new();
            networkObjectInterface.OnDataChange.AddListener(customerScript.HandleObjectReceived);

            //Act=========
            //  Programmatically set the network object
            networkObjectInterface.UpdateData(outgoingObject);

            // ===========Assert=========
            // Check the customer received the same object
            Assert.IsInstanceOf(outgoingObject.GetType(), customerScript.ReceivedObject);
            Assert.AreEqual(customerScript.ReceivedObject, outgoingObject);

            // Check also that HandleObjectReceived was only called once
            Assert.AreEqual(customerScript.ReceivedCounter, 1);

            //Later on, we'll think about how to do an intergration test, where we get the syncer to set the value on the network object 
        }
    }

    internal class NetworkObjectTestPluginScript
    {
        public object ReceivedObject;
        public int ReceivedCounter = 0;

        public virtual void HandleObjectReceived(object obj)
        {
            ReceivedObject = obj;
            ReceivedCounter++;
        }
    }

    internal partial class V_NetworkObjectStub : IV_NetworkObject
    {
        #region State Module Interface
        INetworkObjectStateModule IV_NetworkObject._stateModule => _Service.StateModule;

        public UnityEvent<object> OnDataChange => _Service.StateModule.OnStateChange;
        public object CurrentData => _Service.StateModule.NetworkObject;
        public void UpdateData(object data) => _Service.StateModule.UpdateDataFromPlugin(data);
        #endregion
    }

    internal partial class V_NetworkObjectStub //We can't Substitute.For a MonoBehaviour, so we create an explicit test double class instead 
    {
        protected NetworkObjectService _Service = null;

        public V_NetworkObjectStub(NetworkObjectService service)
        {
            _Service = service;
        }
    }
}
