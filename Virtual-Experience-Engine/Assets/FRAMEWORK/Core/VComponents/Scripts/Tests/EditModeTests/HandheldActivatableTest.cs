using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;


namespace VE2.Core.VComponents.Tests
{
    [TestFixture]
    [Category("Handheld Activatable Tests")]
    internal class HandheldActivatableTests
    {
        private IV_HandheldActivatable _activatablePluginInterface => _v_handheldActivatableProviderStub;
        private V_HandheldActivatableProviderStub _v_handheldActivatableProviderStub;
        private PluginActivatableScript _customerScript;

        //Setup Once for every single test in this test class
        [OneTimeSetUp]
        public void SetUpOnce()
        {
            //Create the activatable
            HandheldActivatableService toggleActivatable = new(
                new HandheldActivatableConfig(), 
                new SingleInteractorActivatableState(), 
                "debug", 
                Substitute.For<IWorldStateSyncService>());

            //Stub out the VC (provider layer) with the activatable
            _v_handheldActivatableProviderStub = new(toggleActivatable);

            //Wire up the customer script to receive the events           
            _customerScript = Substitute.For<PluginActivatableScript>();
            _activatablePluginInterface.OnActivate.AddListener(_customerScript.HandleActivateReceived);
            _activatablePluginInterface.OnDeactivate.AddListener(_customerScript.HandleDeactivateReceived);
        }

        [Test]
        public void HandheldActivatable_WhenActivatedByPlugin_EmitsToPlugin()
        {
            //Wire up the customer script to receive the events
            PluginActivatableScript customerScript = Substitute.For<PluginActivatableScript>();
            _activatablePluginInterface.OnActivate.AddListener(customerScript.HandleActivateReceived);
            _activatablePluginInterface.OnDeactivate.AddListener(customerScript.HandleDeactivateReceived);

            //Invoke click, Check customer received the activation, and that the interactorID is set
            _activatablePluginInterface.IsActivated = true;
            customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(_activatablePluginInterface.IsActivated);
            Assert.AreEqual(_activatablePluginInterface.MostRecentInteractingClientID, ushort.MaxValue);

            // Invoke the click to deactivate
            _activatablePluginInterface.IsActivated = false;
            customerScript.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(_activatablePluginInterface.IsActivated);
            Assert.AreEqual(_activatablePluginInterface.MostRecentInteractingClientID, ushort.MaxValue);
        }
    }

    internal class V_HandheldActivatableProviderStub : IV_HandheldActivatable
    {
        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule IV_HandheldActivatable._StateModule => _HandheldActivatable.StateModule;
        IHandheldClickInteractionModule IV_HandheldActivatable._HandheldClickModule => _HandheldActivatable.HandheldClickInteractionModule;
        #endregion

        internal IHandheldClickInteractionModule HandheldClickInteractionModule => _HandheldActivatable.HandheldClickInteractionModule;  
        protected HandheldActivatableService _HandheldActivatable = null;

        public V_HandheldActivatableProviderStub(HandheldActivatableService HandheldActivatable)
        {
            _HandheldActivatable = HandheldActivatable;
        }

        public void TearDown()
        {
            _HandheldActivatable.TearDown();
        }
    }
}


