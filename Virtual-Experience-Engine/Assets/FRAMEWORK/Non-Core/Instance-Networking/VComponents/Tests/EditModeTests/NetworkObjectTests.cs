using NSubstitute;
using NUnit.Framework;
using VE2.NonCore.Instancing.VComponents.PluginInterfaces;
using VE2.NonCore.Instancing.VComponents.NonInteractableInterfaces;
using VE2.NonCore.Instancing.VComponents.Internal;
using VE2.Common;

namespace VE2.NonCore.Instancing.VComponents.Tests
{
    public class NetworkObjectTests
    {
        [Test]
        public void NetworkObject_WhenObjectIsSet_EmitsToPlugin()
        {
            // ==========Arrange=========
            // Create the NetworkObjectService, injecting default configs 
            NetworkObjectService networkObjectService = new(new NetworkObjectStateConfig(), new NetworkObjectState(), "debug", Substitute.For<WorldStateModulesContainer>());

            // Create a stub for the VC (MonoBehaviour integration layer), injecting the service
            V_NetworkObjectStub v_networkObjectStub = new(networkObjectService);

            // Get the plugin-facing interface out of the VC 
            IV_NetworkObject networkObjectInterface = v_networkObjectStub;

            // Create a substitute for the PluginScript, wire it up to the plugin interface
            PluginScriptMock customerScript = Substitute.For<PluginScriptMock>();
            networkObjectInterface.OnStateChange.AddListener(customerScript.HandleObjectReceived);

            // ===========Act============
            // Create a serializable object to send, which can be an int for now
            System.Random random = new();
            int serializableObject = random.Next(int.MinValue, int.MaxValue);

            // Programmatically set the network object
            networkObjectInterface.NetworkObject = serializableObject;

            // ===========Assert=========
            // Check the customer received the same object
            customerScript.Received(1).HandleObjectReceived(serializableObject);
        }
    }

    public class PluginScriptMock
    {
        public virtual void HandleObjectReceived(object obj) { } //Virtual so the method can be mocked (so we can assert it was called with the right object)
    }

    public class V_NetworkObjectStub : IV_NetworkObject //We can't Substitute.For a MonoBehaviour, so we create an explicit test double class instead 
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
