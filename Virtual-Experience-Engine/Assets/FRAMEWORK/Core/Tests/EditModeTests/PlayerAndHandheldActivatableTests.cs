using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using VE2.Core.Player.Internal;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.Tests;

namespace VE2.Core.Tests
{
    [TestFixture]
    [Category("Player and Handheld Activatable Tests")]
    internal class PlayerAndHandheldActivatableTests : PlayerServiceSetupFixture
    {
        //handheld activatable
        private IV_HandheldActivatable _handheldActivatablePluginInterface => _v_handheldActivatableProviderStub;
        private IHandheldClickInteractionModule _handheldActivatablePlayerInterface => _v_handheldActivatableProviderStub.HandheldClickInteractionModule;
        private V_HandheldActivatableProviderStub _v_handheldActivatableProviderStub;

        //free grabbable
        private IV_FreeGrabbable _grabbablePluginInterface => _v_freeGrabbableProviderStub;
        private IRangedGrabInteractionModuleProvider _grabbableRaycastInterface => _v_freeGrabbableProviderStub;
        private V_FreeGrabbableProviderStub _v_freeGrabbableProviderStub;

        private PluginActivatableScript _customerScript;


        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            //Create the activatable with default values
            HandheldActivatableService handheldActivatable = new(
                new HandheldActivatableConfig(), 
                new SingleInteractorActivatableState(), 
                "debug", 
                Substitute.For<IWorldStateSyncService>());

            //Stub out provider layer
            _v_handheldActivatableProviderStub = new(handheldActivatable);

            //wire up the customer script to receive the events
            _customerScript = Substitute.For<PluginActivatableScript>();
            _handheldActivatablePluginInterface.OnActivate.AddListener(_customerScript.HandleActivateReceived);
            _handheldActivatablePluginInterface.OnDeactivate.AddListener(_customerScript.HandleDeactivateReceived);

            FreeGrabbableService freeGrabbable = new(
                new List<IHandheldInteractionModule>() { _handheldActivatablePlayerInterface },
                new FreeGrabbableConfig(),
                new FreeGrabbableState(),
                "debug",
                Substitute.For<IWorldStateSyncService>(),
                InteractorContainerSetup.InteractorContainer,
                Substitute.For<IRigidbodyWrapper>(),
                new PhysicsConstants());

            //Stub out provider layer
            _v_freeGrabbableProviderStub = new(freeGrabbable);
        }

        [Test]
        public void OnUserClick_WithHandheldActivatable_CustomerScriptReceivesOnActivate()
        {
            //Stub out the raycast provider to hit the activatable GO with 0 range
            RayCastProviderSetup.RaycastProviderStub
                .Raycast(default, default, default, default)
                .ReturnsForAnyArgs(new RaycastResultWrapper(_grabbableRaycastInterface.RangedGrabInteractionModule, null, 0));

            //Wire up the customer script to receive the events
            PluginActivatableScript pluginScriptMock = Substitute.For<PluginActivatableScript>();
            _handheldActivatablePluginInterface.OnActivate.AddListener(pluginScriptMock.HandleActivateReceived);
            _handheldActivatablePluginInterface.OnDeactivate.AddListener(pluginScriptMock.HandleDeactivateReceived);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            Assert.IsTrue(_grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(_grabbablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID);

            //Invoke Activate, Check customer received the activate, and that the interactorID is set
            PlayerInputContainerSetup.HandheldClick2D.OnPressed += Raise.Event<Action>();
            pluginScriptMock.Received(1).HandleActivateReceived();
            Assert.IsTrue(_handheldActivatablePluginInterface.IsActivated);
            Assert.AreEqual(_handheldActivatablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID);

            //Invoke Deactivate, Check customer received the deactivate, and that the interactorID is set
            PlayerInputContainerSetup.HandheldClick2D.OnPressed += Raise.Event<Action>();
            pluginScriptMock.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(_handheldActivatablePluginInterface.IsActivated);
            Assert.AreEqual(_handheldActivatablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();  

            _handheldActivatablePluginInterface.OnActivate.RemoveAllListeners();
            _handheldActivatablePluginInterface.OnDeactivate.RemoveAllListeners();

            _v_handheldActivatableProviderStub.TearDown();
            _v_freeGrabbableProviderStub.TearDown();
        }
    }
}
