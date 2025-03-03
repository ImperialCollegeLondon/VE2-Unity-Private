using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using VE2;
using VE2.Common;
using VE2.Core.Player;
using VE2.Core.Tests;
using VE2.Core.VComponents.InteractableFindables;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.NonInteractableInterfaces;
using VE2.Core.VComponents.PluginInterfaces;
using VE2.Core.VComponents.Tests;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.Tests
{
    [TestFixture]
    [Category("Player and Handheld Adjustable Tests")]
    public class PlayerAndHandheldAdjustableTests
    {
        //handheld adjustable
        private IV_HandheldAdjustable _handheldAdjustablePluginInterface;
        private IHandheldScrollInteractionModule _handheldAdjustablePlayerInterface;
        private IAdjustableStateModule _handheldAdjustableStateModule;
        private V_HandheldAdjustableStub _v_handheldAdjustableStub;
        private HandheldAdjustableConfig _handheldAdjustableConfig;

        //free grabbable
        private IV_FreeGrabbable _grabbablePluginInterface;
        private IRangedGrabPlayerInteractableIntegrator _grabbableRaycastInterface;
        private V_FreeGrabbableStub _v_freeGrabbableStub;

        private PluginAdjustableMock _customerScript;
        private PlayerService _playerServiceStub;

        [OneTimeSetUp]
        public void SetUpOnce()
        {
            //substitute for the customer script
            _customerScript = Substitute.For<PluginAdjustableMock>();
        }

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            //create the handheld adjustable
            _handheldAdjustableConfig = new HandheldAdjustableConfig();
            HandheldAdjustableService handheldAdjustable = HandheldAdjustableServiceStubFactory.Create(_handheldAdjustableConfig);
            _v_handheldAdjustableStub = new(handheldAdjustable);

            //get handheld adjustable interfaces
            _handheldAdjustablePluginInterface = _v_handheldAdjustableStub;
            _handheldAdjustablePlayerInterface = handheldAdjustable.HandheldScrollInteractionModule;
            _handheldAdjustableStateModule = handheldAdjustable.StateModule;

            //wire up the customer script to receive the events
            _handheldAdjustablePluginInterface.OnValueAdjusted.AddListener((value) => _customerScript.HandleValueAdjusted(value));

            //create the free grabbable
            FreeGrabbableService freeGrabbableService = FreeGrabbableServiceStubFactory.Create(
                handheldInteractionModules: new List<IHandheldInteractionModule> { _handheldAdjustablePlayerInterface },
                interactorContainer: InteractorSetup.InteractorContainerStub);

            _v_freeGrabbableStub = new(freeGrabbableService);

            //get grabbable interfaces
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
        public void WithHandheldAdjustable_OnUserScroll_CustomerScriptReceiveOnValueAdjusted([Random(-100f, -1, 1)] float minValue, [Random(1f, 100f, 1)] float maxValue)
        {
            //get starting and increment values
            float startingValue = _handheldAdjustableConfig.StateConfig.StartingOutputValue;
            float increment = _handheldAdjustableConfig.StateConfig.IncrementPerScrollTick;
            
            //assign min and max values
            _handheldAdjustableStateModule.MinimumOutputValue = minValue;
            _handheldAdjustableStateModule.MaximumOutputValue = maxValue;

            //stub out the raycast result to return the grabbable's interaction module
            RayCastProviderSetup.StubRangedInteractionModuleForRaycastProviderStub(_grabbableRaycastInterface.RangedGrabInteractionModule);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            InputHandlerSetup.PlayerInputContainerStubWrapper.Grab2D.OnPressed += Raise.Event<Action>();
            Assert.IsTrue(_grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(_grabbablePluginInterface.MostRecentInteractingClientID, MultiplayerSupportSetup.LocalClientID);

            //Invoke scroll up, check customer received the scroll up, and that the value is correct
            InputHandlerSetup.PlayerInputContainerStubWrapper.ScrollTickUp2D.OnTickOver += Raise.Event<Action>();
            _customerScript.Received(1).HandleValueAdjusted(startingValue + increment);
            Assert.IsTrue(_handheldAdjustablePluginInterface.Value == startingValue + increment);
            Assert.AreEqual(_handheldAdjustablePluginInterface.MostRecentInteractingClientID, MultiplayerSupportSetup.LocalClientID);

            //Invoke scroll down, check customer received the scroll down, and that the value is correct
            InputHandlerSetup.PlayerInputContainerStubWrapper.ScrollTickDown2D.OnTickOver += Raise.Event<Action>();
            _customerScript.Received(1).HandleValueAdjusted(startingValue);
            Assert.IsTrue(_handheldAdjustablePluginInterface.Value == startingValue);
            Assert.AreEqual(_handheldAdjustablePluginInterface.MostRecentInteractingClientID, MultiplayerSupportSetup.LocalClientID);

            //Invoke scroll down, check customer received the scroll down, and that the value is correct
            InputHandlerSetup.PlayerInputContainerStubWrapper.ScrollTickDown2D.OnTickOver += Raise.Event<Action>();
            _customerScript.Received(1).HandleValueAdjusted(startingValue - increment);
            Assert.IsTrue(_handheldAdjustablePluginInterface.Value == startingValue - increment);
            Assert.AreEqual(_handheldAdjustablePluginInterface.MostRecentInteractingClientID, MultiplayerSupportSetup.LocalClientID);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();

            _handheldAdjustablePluginInterface.OnValueAdjusted.RemoveAllListeners();

            _v_handheldAdjustableStub.TearDown();
            _handheldAdjustablePluginInterface = null;
            _handheldAdjustablePlayerInterface = null;

            _v_freeGrabbableStub.TearDown();
            _grabbablePluginInterface = null;
            _grabbableRaycastInterface = null;

            _playerServiceStub.TearDown();
        }

        [OneTimeTearDown]
        public void TearDownOnce() { }
    }
}
