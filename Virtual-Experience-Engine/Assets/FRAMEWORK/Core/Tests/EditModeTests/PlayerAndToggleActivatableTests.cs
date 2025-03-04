using NSubstitute;
using NUnit.Framework;
using System;
using VE2.Core.VComponents.Tests;
using UnityEngine;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.API;

namespace VE2.Core.Tests
{
    [TestFixture]
    [Category("Player and Toggle Activatable Tests")]
    internal class PlayerAndToggleActivatableTests : PlayerServiceSetupFixture
    {
        //variables that will be reused in the tests
        private IV_ToggleActivatable _activatablePluginInterface => _v_activatableProviderStub;
        private IRangedClickInteractionModuleProvider _activatableRaycastInterface => _v_activatableProviderStub;
        private V_ToggleActivatableProviderStub _v_activatableProviderStub;
        private PluginActivatableScript _customerScript;

        //Setup Once for every single test in this test fixture
        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            //Create the activatable
            ToggleActivatableService toggleActivatableService = new(
                new ToggleActivatableConfig(),
                new SingleInteractorActivatableState(),
                "debug",
                Substitute.For<IWorldStateSyncService>());
            
            //Stub out the provider layer
            _v_activatableProviderStub = new(toggleActivatableService);

            //Wire up the customer script to receive the events           
            _customerScript = Substitute.For<PluginActivatableScript>();
            _activatablePluginInterface.OnActivate.AddListener(_customerScript.HandleActivateReceived);
            _activatablePluginInterface.OnDeactivate.AddListener(_customerScript.HandleDeactivateReceived);
        }

        //test method to confirm that the activatable emits the correct events when the player interacts with it
        [Test]
        public void OnUserClick_WithHoveringActivatable_CustomerScriptReceivesOnActivate( [Random((ushort) 0, ushort.MaxValue, 1)] ushort localClientID)
        {
            RayCastProviderSetup.StubRangedInteractionModuleForRaycastProviderStub(_activatableRaycastInterface.RangedClickInteractionModule);
            LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID.Returns(localClientID);

            //Check customer received the activation, and that the interactorID is set
            PlayerInputContainerSetup.RangedClick2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(_activatablePluginInterface.IsActivated, "Activatable should be activated");
            Assert.AreEqual(_activatablePluginInterface.MostRecentInteractingClientID, localClientID);

            // Invoke the click to deactivate
            PlayerInputContainerSetup.RangedClick2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(_activatablePluginInterface.IsActivated, "Activatable should be deactivated");
            Assert.AreEqual(_activatablePluginInterface.MostRecentInteractingClientID, localClientID);
        }

        //tear down that runs after every test method in this test fixture
        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();
            
            _activatablePluginInterface.OnActivate.RemoveAllListeners();
            _activatablePluginInterface.OnDeactivate.RemoveAllListeners();

            _v_activatableProviderStub.TearDown();
        }
    }
}
