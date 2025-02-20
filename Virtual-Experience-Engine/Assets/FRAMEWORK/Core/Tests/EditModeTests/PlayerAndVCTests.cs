using NSubstitute;
using NUnit.Framework;
using System;
using VE2.Core.VComponents.Tests;
using UnityEngine;
using VE2.Core.VComponents.Internal;
using VE2.Core.Common;
using VE2.Core.VComponents.API;


namespace VE2.Core.Tests
{
    [TestFixture]
    [Category("Player and ToggleActivatable Tests")]
    internal class PlayerAndToggleActivatableTests : PlayerServiceSetupFixture
    {
        //variables that will be reused in the tests
        private IV_ToggleActivatable _activatablePluginInterface;
        private PluginScriptMock _customerScript;
        private V_ToggleActivatableStub _v_activatableStub;
        private IRangedClickInteractionModuleProvider _activatableRaycastInterface;

        //Setup Once for every single test in this test fixture
        [OneTimeSetUp]
        public void SetUpOnce()
        {
            //Create the activatable
            ToggleActivatableService toggleActivatableService = ToggleActivatableServiceStubFactory.Create();
            _v_activatableStub = new(toggleActivatableService);

            //hook up interfaces
            _activatablePluginInterface = _v_activatableStub;
            _activatableRaycastInterface = _v_activatableStub;

            //Wire up the customer script to receive the events           
            _customerScript = Substitute.For<PluginScriptMock>();
            _activatablePluginInterface.OnActivate.AddListener(_customerScript.HandleActivateReceived);
            _activatablePluginInterface.OnDeactivate.AddListener(_customerScript.HandleDeactivateReceived);
        }

        //setup that runs before every test method in this test fixture
        [SetUp]
        public void SetUpBeforeEveryTest() { }

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
            _activatablePluginInterface.IsActivated = false;
        }

        //tear down that runs once after all the tests in this class
        [OneTimeTearDown]
        public void TearDownOnce()
        {
            _activatablePluginInterface.OnActivate.RemoveAllListeners();
            _activatablePluginInterface.OnDeactivate.RemoveAllListeners();

            _v_activatableStub.TearDown();
        }
    }
}
