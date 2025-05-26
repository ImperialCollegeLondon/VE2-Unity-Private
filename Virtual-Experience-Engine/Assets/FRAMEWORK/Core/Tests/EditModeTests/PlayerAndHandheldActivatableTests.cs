using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using VE2.Common.Shared;
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

        private IV_HandheldActivatable _handheldHoldActivatablePluginInterface => _v_handheldHoldActivatableProviderStub;
        private IHandheldClickInteractionModule _handheldHoldActivatablePlayerInterface => _v_handheldHoldActivatableProviderStub.HandheldClickInteractionModule;
        private V_HandheldActivatableProviderStub _v_handheldHoldActivatableProviderStub;

        //free grabbable
        private IV_FreeGrabbable _grabbablePluginInterface => _v_freeGrabbableProviderStub;
        private IRangedGrabInteractionModuleProvider _grabbableRaycastInterface => _v_freeGrabbableProviderStub;
        private V_FreeGrabbableProviderStub _v_freeGrabbableProviderStub;

        private PluginActivatableScript _customerScript;

        //free grabbable for hold interaction
        private IV_FreeGrabbable _grabbable2PluginInterface => _v_freeGrabbable2ProviderStub;
        private IRangedGrabInteractionModuleProvider _grabbable2RaycastInterface => _v_freeGrabbable2ProviderStub;
        private V_FreeGrabbableProviderStub _v_freeGrabbable2ProviderStub;

        private PluginActivatableScript _customerScript2;

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            //Stub out provider layer
            _v_handheldActivatableProviderStub = new();

            //Stub out provider layer
            _v_freeGrabbableProviderStub = new();


            //Create the activatable with default values
            HandheldActivatableService handheldActivatable = new(
                _v_freeGrabbableProviderStub,
                new HandheldActivatableConfig(), 
                new SingleInteractorActivatableState(), 
                "testHandHeldActivatable", 
                Substitute.For<IWorldStateSyncableContainer>(),
                new ActivatableGroupsContainer(),
                LocalClientIDWrapperSetup.LocalClientIDWrapper);

            //Set the HandheldActivatableService to the provider stub NOTE: This is a bit of a hack because MonoBehaviours are used to initialise both the FreeGrabbable and HandheldActivatable services.
            _v_handheldActivatableProviderStub.Service = handheldActivatable;

            FreeGrabbableService freeGrabbable = new(
                new List<IHandheldInteractionModule>() { _handheldActivatablePlayerInterface },
                new FreeGrabbableConfig(),
                new GrabbableState(),
                "testGrabbable",
                Substitute.For<IWorldStateSyncableContainer>(),
                GrabInteractableContainerSetup.GrabInteractableContainer,
                InteractorContainerSetup.InteractorContainer,
                Substitute.For<IRigidbodyWrapper>(),
                new PhysicsConstants(),
                new V_FreeGrabbable(),
                LocalClientIDWrapperSetup.LocalClientIDWrapper);

            _v_freeGrabbableProviderStub.Service = freeGrabbable;


            //Stub out provider layer
            _v_handheldHoldActivatableProviderStub = new();

            //Stub out provider layer
            _v_freeGrabbable2ProviderStub = new();

            //Create the activatable hold with default values
            HandheldActivatableConfig handheldActivatableConfig = new()
            {
                StateConfig = new ToggleActivatableStateConfig(),
                HandheldClickInteractionConfig = new()
                {
                    IsHoldMode = true
                },
                GeneralInteractionConfig = new GeneralInteractionConfig()
            };

            HandheldActivatableService handheldActivatableHold = new(
                _v_freeGrabbable2ProviderStub,
                handheldActivatableConfig,
                new SingleInteractorActivatableState(),
                "testHoldActivatable",
                Substitute.For<IWorldStateSyncableContainer>(),
                new ActivatableGroupsContainer(),
                LocalClientIDWrapperSetup.LocalClientIDWrapper);

            _v_handheldHoldActivatableProviderStub.Service = handheldActivatableHold;

            FreeGrabbableService freeGrabbable2 = new(
                new List<IHandheldInteractionModule>() { _handheldHoldActivatablePlayerInterface },
                new FreeGrabbableConfig(),
                new GrabbableState(),
                "testGrabbable2",
                Substitute.For<IWorldStateSyncableContainer>(),
                GrabInteractableContainerSetup.GrabInteractableContainer,
                InteractorContainerSetup.InteractorContainer,
                Substitute.For<IRigidbodyWrapper>(),
                new PhysicsConstants(),
                new V_FreeGrabbable(),
                LocalClientIDWrapperSetup.LocalClientIDWrapper);

            _v_freeGrabbable2ProviderStub.Service = freeGrabbable2;

            //wire up the customer script to receive the events
            _customerScript = Substitute.For<PluginActivatableScript>();
            _handheldActivatablePluginInterface.OnActivate.AddListener(_customerScript.HandleActivateReceived);
            _handheldActivatablePluginInterface.OnDeactivate.AddListener(_customerScript.HandleDeactivateReceived);

            //wire up the customer script to receive the events for the hold activatable interaction
            _customerScript2 = Substitute.For<PluginActivatableScript>();
            _handheldHoldActivatablePluginInterface.OnActivate.AddListener(_customerScript2.HandleActivateReceived);
            _handheldHoldActivatablePluginInterface.OnDeactivate.AddListener(_customerScript2.HandleDeactivateReceived);
        }

        [Test]
        public void OnUserClick_WithHandheldActivatable_CustomerScriptReceivesOnActivate()
        {
            //Stub out the raycast provider to hit the activatable GO with 0 range
            RayCastProviderSetup.RaycastProviderStub
                .Raycast(default, default, default, default)
                .ReturnsForAnyArgs(new RaycastResultWrapper(_grabbableRaycastInterface.RangedGrabInteractionModule, null, 0, true));

            //Wire up the customer script to receive the events
            PluginActivatableScript pluginScriptMock = Substitute.For<PluginActivatableScript>();
            _handheldActivatablePluginInterface.OnActivate.AddListener(pluginScriptMock.HandleActivateReceived);
            _handheldActivatablePluginInterface.OnDeactivate.AddListener(pluginScriptMock.HandleDeactivateReceived);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            Assert.IsTrue(_grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(_grabbablePluginInterface.MostRecentInteractingClientID.Value, LocalClientIDWrapperSetup.LocalClientIDWrapper.Value);
            Assert.IsTrue(_grabbablePluginInterface.MostRecentInteractingClientID.IsLocal);

            //Invoke Activate, Check customer received the activate, and that the interactorID is set
            PlayerInputContainerSetup.HandheldClick2D.OnPressed += Raise.Event<Action>();
            pluginScriptMock.Received(1).HandleActivateReceived();
            Assert.IsTrue(_handheldActivatablePluginInterface.IsActivated);
            Assert.AreEqual(_handheldActivatablePluginInterface.MostRecentInteractingClientID.Value, LocalClientIDWrapperSetup.LocalClientIDWrapper.Value);
            Assert.IsTrue(_grabbablePluginInterface.MostRecentInteractingClientID.IsLocal);

            //Invoke Deactivate, Check customer received the deactivate, and that the interactorID is set
            PlayerInputContainerSetup.HandheldClick2D.OnPressed += Raise.Event<Action>();
            pluginScriptMock.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(_handheldActivatablePluginInterface.IsActivated);
            Assert.AreEqual(_handheldActivatablePluginInterface.MostRecentInteractingClientID.Value, LocalClientIDWrapperSetup.LocalClientIDWrapper.Value);
            Assert.IsTrue(_grabbablePluginInterface.MostRecentInteractingClientID.IsLocal);
        }

        [Test]
        public void OnUserClick_WithHandheldHoldActivatable_CustomerScriptReceivesOnActivate()
        {
            // Stub out the raycast provider so that it "hits" the activatable GameObject with 0 range.
            RayCastProviderSetup.RaycastProviderStub
                .Raycast(default, default, default, default)
                .ReturnsForAnyArgs(new RaycastResultWrapper(_grabbable2RaycastInterface.RangedGrabInteractionModule, null, 0, true));

            // Wire up the customer script (using a NSubstitute mock) to receive the events on the hold activatable interface.
            PluginActivatableScript pluginScriptMock = Substitute.For<PluginActivatableScript>();
            _handheldHoldActivatablePluginInterface.OnActivate.AddListener(pluginScriptMock.HandleActivateReceived);
            _handheldHoldActivatablePluginInterface.OnDeactivate.AddListener(pluginScriptMock.HandleDeactivateReceived);

            // Simulate the grab input event.
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            Assert.IsTrue(_grabbable2PluginInterface.IsGrabbed);
            Assert.AreEqual(LocalClientIDWrapperSetup.LocalClientID,
                            _grabbable2PluginInterface.MostRecentInteractingClientID.Value);

            // Simulate the user click to activate the hold activatable.
            PlayerInputContainerSetup.HandheldClick2D.OnPressed += Raise.Event<Action>();
            pluginScriptMock.Received(1).HandleActivateReceived();
            Assert.IsTrue(_handheldHoldActivatablePluginInterface.IsActivated);
            Assert.AreEqual(LocalClientIDWrapperSetup.LocalClientID,
                            _handheldHoldActivatablePluginInterface.MostRecentInteractingClientID.Value);

            // Simulate the user release to deactivate it (using OnRelease).
            PlayerInputContainerSetup.HandheldClick2D.OnReleased += Raise.Event<Action>();
            pluginScriptMock.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(_handheldHoldActivatablePluginInterface.IsActivated);
            Assert.AreEqual(LocalClientIDWrapperSetup.LocalClientID,
                            _handheldHoldActivatablePluginInterface.MostRecentInteractingClientID.Value);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();

            _handheldActivatablePluginInterface.OnActivate.RemoveAllListeners();
            _handheldActivatablePluginInterface.OnDeactivate.RemoveAllListeners();

            _v_handheldActivatableProviderStub.TearDown();
            _v_freeGrabbableProviderStub.TearDown();
            _v_freeGrabbable2ProviderStub.TearDown();
        }
    }
}
