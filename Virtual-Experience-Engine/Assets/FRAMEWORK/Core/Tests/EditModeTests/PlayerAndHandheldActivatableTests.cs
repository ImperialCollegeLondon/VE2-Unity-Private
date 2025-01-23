using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using VE2.Core.Player;
using VE2.Core.VComponents.InteractableFindables;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.PluginInterfaces;
using VE2.Core.VComponents.Tests;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.Tests
{
    [TestFixture]
    [Category("Player and Handheld Activatable Tests")]
    public class PlayerAndHandheldActivatableTests
    {
        //handheld activatable
        private IV_HandheldActivatable _handheldActivatablePluginInterface;
        private IHandheldClickInteractionModule _handheldActivatablePlayerInterface;
        private V_HandheldActivatableStub _v_handheldActivatableStub;

        //free grabbable
        private IV_FreeGrabbable _grabbablePluginInterface;
        private IRangedGrabPlayerInteractableIntegrator _grabbableRaycastInterface;
        private V_FreeGrabbableStub _v_freeGrabbableStub;

        private PluginActivatableMock _customerScript;
        private PlayerService _playerServiceStub;

        [OneTimeSetUp]
        public void SetUpOnce()
        {
            //substitute for the customer script
            _customerScript = Substitute.For<PluginActivatableMock>();
        }

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            //create the handheld activatable
            HandheldActivatableService handheldActivatable = HandheldActivatableServiceStubFactory.Create();

            _v_handheldActivatableStub = new(handheldActivatable);

            //get handheld activatable interfaces
            _handheldActivatablePluginInterface = _v_handheldActivatableStub;
            _handheldActivatablePlayerInterface = handheldActivatable.HandheldClickInteractionModule;

            //wire up the customer script to receive the events
            _handheldActivatablePluginInterface.OnActivate.AddListener(_customerScript.HandleActivateReceived);
            _handheldActivatablePluginInterface.OnDeactivate.AddListener(_customerScript.HandleDeactivateReceived);

            //create the free grabbable
            FreeGrabbableService freeGrabbableService = FreeGrabbableServiceStubFactory.Create(
                handheldInteractionModules: new List<IHandheldInteractionModule> {_handheldActivatablePlayerInterface},
                interactorContainer: InteractorSetup.InteractorContainerStub);

            _v_freeGrabbableStub = new(freeGrabbableService);

            //get free grabbable interfaces
            _grabbablePluginInterface = _v_freeGrabbableStub;
            _grabbableRaycastInterface = _v_freeGrabbableStub;

            //create the player service
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

        [Test]
        public void WithHandheldActivatable_OnUserClick_CustomerScriptReceivesOnActivate()
        {
            //stub out the raycast result to return the grabbable's interaction module
            RayCastProviderSetup.StubRangedInteractionModuleForRaycastProviderStub(_grabbableRaycastInterface.RangedGrabInteractionModule);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            InputHandlerSetup.PlayerInputContainerStubWrapper.Grab2D.OnPressed += Raise.Event<Action>();
            Assert.IsTrue(_grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(_grabbablePluginInterface.MostRecentInteractingClientID, MultiplayerSupportSetup.LocalClientID);

            //Invoke Activate, Check customer received the activate, and that the interactorID is set
            InputHandlerSetup.PlayerInputContainerStubWrapper.HandheldClick2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(_handheldActivatablePluginInterface.IsActivated);
            Assert.AreEqual(_handheldActivatablePluginInterface.MostRecentInteractingClientID, MultiplayerSupportSetup.LocalClientID);

            //Invoke Deactivate, Check customer received the deactivate, and that the interactorID is set
            InputHandlerSetup.PlayerInputContainerStubWrapper.HandheldClick2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(_handheldActivatablePluginInterface.IsActivated);
            Assert.AreEqual(_handheldActivatablePluginInterface.MostRecentInteractingClientID, MultiplayerSupportSetup.LocalClientID);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();  

            _handheldActivatablePluginInterface.OnActivate.RemoveAllListeners();
            _handheldActivatablePluginInterface.OnDeactivate.RemoveAllListeners();

            _v_handheldActivatableStub.TearDown();
            _handheldActivatablePluginInterface = null;

            _v_freeGrabbableStub.TearDown();
            _grabbablePluginInterface = null;   
            _grabbableRaycastInterface = null;

            _playerServiceStub.TearDown();
        }

        [OneTimeTearDown]
        public void TearDownOnce() { }
    }
}
