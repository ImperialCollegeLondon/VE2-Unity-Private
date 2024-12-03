using NSubstitute;
using NUnit.Framework;
using System;
using VE2.Core.VComponents.Tests;
using VE2.Core.VComponents.PluginInterfaces;
using UnityEngine;
using VE2.Core.VComponents.InteractableFindables;
using VE2.Core.VComponents.Internal;
using VE2.Core.Common;
using VE2.Core.Player;
using VE2.Common;


namespace VE2.Core.Tests
{
    [TestFixture]
    [Category("Player and ToggleActivatable Tests")]
    public class PlayerAndToggleActivatableTests
    {
        private IV_ToggleActivatable _activatablePluginInterface;
        private PluginActivatableMock _customerScript;
        private V_ToggleActivatableStub _v_activatableStub;
        private IRangedClickPlayerInteractableIntegrator _activatableRaycastInterface;
        private PlayerService _playerServiceStub;

        //Setup Once for every single test in this test fixture
        [OneTimeSetUp]
        public void SetUpOnce()
        {
            //Wire up the customer script to receive the events           
            _customerScript = Substitute.For<PluginActivatableMock>();

        }

        //setup that runs before every test method in this test fixture
        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            //Create the activatable
            ToggleActivatableService toggleActivatableService = ToggleActivatableServiceStubFactory.Create();
            _v_activatableStub = new(toggleActivatableService);

            //hook up interfaces
            _activatablePluginInterface = _v_activatableStub;
            _activatableRaycastInterface = _v_activatableStub;

            _activatablePluginInterface.OnActivate.AddListener(_customerScript.HandleActivateReceived);
            _activatablePluginInterface.OnDeactivate.AddListener(_customerScript.HandleDeactivateReceived);

            _playerServiceStub = new(
                new PlayerTransformData(),
                new PlayerStateConfig(),
                false,
                true,
                new PlayerStateModuleContainer(),
                InteractorSetup.InteractorContainerStub,
                PlayerSettingsProviderSetup.PlayerSettingsProviderStub,
                Substitute.For<IPlayerAppearanceOverridesProvider>(),
                MultiplayerSupportSetup.MultiplayerSupportStub,
                InputHandlerSetup.PlayerInputContainerStubWrapper.PlayerInputContainer,
                RayCastProviderSetup.RaycastProviderStub, 
                Substitute.For<IXRManagerWrapper>()
            );
        }

        //test method to confirm that the activatable emits the correct events when the player interacts with it
        [Test]
        public void WithHoveringActivatable_OnUserClick_CustomerScriptReceivesOnActivate()
        {
            RayCastProviderSetup.StubRangedInteractionModuleForRaycastProviderStub(_activatableRaycastInterface.RangedClickInteractionModule);

            //Check customer received the activation, and that the interactorID is set
            InputHandlerSetup.PlayerInputContainerStubWrapper.RangedClick2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(_activatablePluginInterface.IsActivated, "Activatable should be activated");
            Assert.AreEqual(_activatablePluginInterface.MostRecentInteractingClientID, MultiplayerSupportSetup.LocalClientID);

            // Invoke the click to deactivate
            InputHandlerSetup.PlayerInputContainerStubWrapper.RangedClick2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(_activatablePluginInterface.IsActivated, "Activatable should be deactivated");
            Assert.AreEqual(_activatablePluginInterface.MostRecentInteractingClientID, MultiplayerSupportSetup.LocalClientID);
        }

        //tear down that runs after every test method in this test fixture
        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();

            _activatablePluginInterface.OnActivate.RemoveAllListeners();
            _activatablePluginInterface.OnDeactivate.RemoveAllListeners();            

            _v_activatableStub.TearDown();
            _activatablePluginInterface = null;
            _activatableRaycastInterface = null;
            
            _playerServiceStub.TearDown();
        }

        //tear down that runs once after all the tests in this class
        [OneTimeTearDown]
        public void TearDownOnce() { }
    }
}
