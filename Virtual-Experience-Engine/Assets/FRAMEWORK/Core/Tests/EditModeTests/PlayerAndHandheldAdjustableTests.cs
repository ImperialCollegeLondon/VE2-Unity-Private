using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VE2.Core.Player.API;
using VE2.Core.Player.Internal;
using VE2.Core.Tests;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.Tests;

namespace VE2.Core.Tests
{
    [TestFixture]
    internal class PlayerAndHandheldAdjustableTests  : PlayerServiceSetupFixture
    {
        [Test]
        public void OnUserScroll_WithHandheldAdjustable_CustomerScriptReceivesOnValueAdjusted()
        {
            HandheldAdjustableConfig handheldAdjustableConfig = new();

            float startingValue = handheldAdjustableConfig.StateConfig.StartingValue;
            float increment = handheldAdjustableConfig.HandheldAdjustableServiceConfig.IncrementPerScrollTick;

            System.Random random = new();
            handheldAdjustableConfig.StateConfig.MaximumValue = random.Next(0, 100);
            handheldAdjustableConfig.StateConfig.MinimumValue = random.Next(-100, 0);

            //Create the activatable with above random values
            HandheldAdjustableService handheldAdjustable = new(handheldAdjustableConfig, new AdjustableState(), "debug", Substitute.For<IWorldStateSyncService>());

            //Stub out the VC (integration layer) with the activatable
            V_HandheldAdjustableStub v_handheldAdjustableStub = new(handheldAdjustable);

            //Get interfaces
            IV_HandheldAdjustable handheldAdjustablePluginInterface = v_handheldAdjustableStub;
            IHandheldScrollInteractionModule handheldAdjustablePlayerInterface = handheldAdjustable.HandheldScrollInteractionModule;

            FreeGrabbableService freeGrabbable = new(
                new List<IHandheldInteractionModule>() { handheldAdjustablePlayerInterface },
                new FreeGrabbableConfig(),
                new FreeGrabbableState(),
                "debug",
                Substitute.For<IWorldStateSyncService>(),
                InteractorContainerSetup.InteractorContainer,
                Substitute.For<IRigidbodyWrapper>(),
                new PhysicsConstants());

            //Stub out the VC (integration layer) with the grabbable
            V_FreeGrabbableStub v_freeGrabbableStub = new(freeGrabbable);

            //Get interfaces
            IV_FreeGrabbable grabbablePluginInterface = v_freeGrabbableStub;
            IRangedGrabInteractionModuleProvider grabbableRaycastInterface = v_freeGrabbableStub;
            IRangedGrabInteractionModule grabbablePlayerInterface = grabbableRaycastInterface.RangedGrabInteractionModule;

            //Stub out the raycast provider to hit the activatable GO with 0 range
            RayCastProviderSetup.RaycastProviderStub
                .Raycast(default, default, default, default)
                .ReturnsForAnyArgs(new RaycastResultWrapper(grabbablePlayerInterface, null, 0));

            //Wire up the customer script to receive the events
            PluginScriptMock pluginScriptMock = Substitute.For<PluginScriptMock>();
            handheldAdjustablePluginInterface.OnValueAdjusted.AddListener((value) => pluginScriptMock.HandleValueAdjusted(value));

            //Invoke grab, check customer received the grab, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            Assert.IsTrue(grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(grabbablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID);

            //Invoke scroll up, check customer received the scroll up, and that the value is correct
            PlayerInputContainerSetup.ScrollTickUp2D.OnTickOver += Raise.Event<Action>();
            pluginScriptMock.Received(1).HandleValueAdjusted(startingValue + increment);
            Assert.IsTrue(handheldAdjustablePluginInterface.Value == startingValue + increment);
            Assert.AreEqual(handheldAdjustablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID);

            //Invoke scroll down, check customer received the scroll down, and that the value is correct
            PlayerInputContainerSetup.ScrollTickDown2D.OnTickOver += Raise.Event<Action>();
            pluginScriptMock.Received(1).HandleValueAdjusted(startingValue);
            Assert.IsTrue(handheldAdjustablePluginInterface.Value == startingValue);
            Assert.AreEqual(handheldAdjustablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID);

            //Invoke scroll down, check customer received the scroll down, and that the value is correct
            PlayerInputContainerSetup.ScrollTickDown2D.OnTickOver += Raise.Event<Action>();
            pluginScriptMock.Received(1).HandleValueAdjusted(startingValue - increment);
            Assert.IsTrue(handheldAdjustablePluginInterface.Value == startingValue - increment);
            Assert.AreEqual(handheldAdjustablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID);
        }
    }
}