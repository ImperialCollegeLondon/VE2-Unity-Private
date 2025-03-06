using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.Tests;

namespace VE2.Core.Tests
{
    [TestFixture]
    [Category("Player and Handheld Adjustable Tests")]
    internal class PlayerAndHandheldAdjustableTests  : PlayerServiceSetupFixture
    {
        //handheld adjustable
        private IV_HandheldAdjustable _handheldAdjustablePluginInterface => _v_handheldAdjustableStub;
        private IHandheldScrollInteractionModule _handheldAdjustablePlayerInterface =>  _v_handheldAdjustableStub.HandheldScrollInteractionModule;
        private V_HandheldAdjustableProviderStub _v_handheldAdjustableStub;
        private HandheldAdjustableConfig _handheldAdjustableConfig; //TODO: Should go in via plugin interface rather than talking directly to the config object

        //free grabbable
        private IV_FreeGrabbable _grabbablePluginInterface => _v_freeGrabbableStub;
        private IRangedGrabInteractionModuleProvider _grabbableRaycastInterface => _v_freeGrabbableStub;
        private V_FreeGrabbableProviderStub _v_freeGrabbableStub;

        private PluginAdjustableScript _customerScript;

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            _handheldAdjustableConfig = new();

            //Create the activatable with above random values
            HandheldAdjustableService handheldAdjustable = new(_handheldAdjustableConfig, new AdjustableState(), "debug", Substitute.For<IWorldStateSyncService>());

            //Stub out the VC (provider layer) with the activatable
            _v_handheldAdjustableStub = new(handheldAdjustable);

            //wire up the customer script to receive the events
            _customerScript = Substitute.For<PluginAdjustableScript>();
            _handheldAdjustablePluginInterface.OnValueAdjusted.AddListener((value) => _customerScript.HandleValueAdjusted(value));

            //Create the grabbable, with a link to the adjustable
            FreeGrabbableService freeGrabbable = new(
                new List<IHandheldInteractionModule>() { _handheldAdjustablePlayerInterface },
                new FreeGrabbableConfig(),
                new FreeGrabbableState(),
                "debug",
                Substitute.For<IWorldStateSyncService>(),
                InteractorContainerSetup.InteractorContainer,
                Substitute.For<IRigidbodyWrapper>(),
                new PhysicsConstants());

            //Stub out the VC (provider layer) with the grabbable
            _v_freeGrabbableStub = new(freeGrabbable);
        }


        [Test]
        public void WithHandheldAdjustable_OnUserScroll_CustomerScriptReceiveOnValueAdjusted([Random(-100f, 0f, 1)] float minValue, [Random(0f, 100f, 1)] float maxValue)
        {
            //get starting and increment values
            float startingValue = _handheldAdjustableConfig.StateConfig.StartingValue;
            float increment = _handheldAdjustableConfig.HandheldAdjustableServiceConfig.IncrementPerScrollTick;

            //assign min and max values
            _handheldAdjustableConfig.StateConfig.MinimumValue = minValue;
            _handheldAdjustableConfig.StateConfig.MaximumValue = maxValue;

            //stub out the raycast result to return the grabbable's interaction module
            RayCastProviderSetup.StubRangedInteractionModuleForRaycastProviderStub(_grabbableRaycastInterface.RangedGrabInteractionModule);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            Assert.IsTrue(_grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(_grabbablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID);

            //Invoke scroll up, check customer received the scroll up, and that the value is correct
            PlayerInputContainerSetup.ScrollTickUp2D.OnTickOver += Raise.Event<Action>();
            _customerScript.Received(1).HandleValueAdjusted(startingValue + increment);
            Assert.IsTrue(_handheldAdjustablePluginInterface.Value == startingValue + increment);
            Assert.AreEqual(_handheldAdjustablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID);

            //Invoke scroll down, check customer received the scroll down, and that the value is correct
            PlayerInputContainerSetup.ScrollTickDown2D.OnTickOver += Raise.Event<Action>();
            _customerScript.Received(1).HandleValueAdjusted(startingValue);
            Assert.IsTrue(_handheldAdjustablePluginInterface.Value == startingValue);
            Assert.AreEqual(_handheldAdjustablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID);

            //Invoke scroll down, check customer received the scroll down, and that the value is correct
            PlayerInputContainerSetup.ScrollTickDown2D.OnTickOver += Raise.Event<Action>();
            _customerScript.Received(1).HandleValueAdjusted(startingValue - increment);
            Assert.IsTrue(_handheldAdjustablePluginInterface.Value == startingValue - increment);
            Assert.AreEqual(_handheldAdjustablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();

            _handheldAdjustablePluginInterface.OnValueAdjusted.RemoveAllListeners();

            _v_handheldAdjustableStub.TearDown();
            _v_freeGrabbableStub.TearDown();
        }
    }
}