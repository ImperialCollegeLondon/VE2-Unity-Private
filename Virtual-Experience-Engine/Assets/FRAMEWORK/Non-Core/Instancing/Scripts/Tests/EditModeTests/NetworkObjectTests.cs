using NSubstitute;
using NUnit.Framework;
using VE2.NonCore.Instancing.API;
using VE2.NonCore.Instancing.Internal;

namespace VE2.NonCore.Instancing.VComponents.Tests
{
    internal class NetworkObjectTests
    {
        [Test]
        public void NetworkObject_WhenObjectIsSet_EmitsToPlugin()
        {
            //Arrange=========
            //  Create the NetworkObjectService, injecting default configs 
            //  Create a stub for the VC (MonoBehaviour integration layer), injecting the service (See PushActivatableTests for this)
            //  Get the plugin-facing interface out of the VC 
            //  Create a substitute for the PluginScript, wire it up to the plugin interface 

            //Act=========
            //  Programmatically set the network object 

            //Assert=========
            //  Check the plugin script mock reveived the same object back 

            //Later on, we'll think about how to do an intergration test, where we get the syncer to set the value on the network object 
        }
    }

    internal class PluginScript
    {
        public virtual void HandleObjectReceived(object obj) { } //Virtual so the method can be mocked (so we can assert it was called with the right object)
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
