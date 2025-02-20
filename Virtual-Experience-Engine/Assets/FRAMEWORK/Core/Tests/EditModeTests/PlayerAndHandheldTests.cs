using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using VE2.Core.Player.API;
using VE2.Core.Player.Internal;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.Tests;
using static VE2.Core.Common.CommonSerializables;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Tests
{
    [TestFixture]
    internal class PlayerAndHandheldActivatableTests : PlayerServiceSetupFixture
    {
        [Test]
        public void OnUserClick_WithHandheldActivatable_CustomerScriptReceivesOnActivate()
        {
            //Create the activatable with default values
            HandheldActivatableService handheldActivatable = new(new HandheldActivatableConfig(), new SingleInteractorActivatableState(), "debug", Substitute.For<IWorldStateSyncService>());

            //Stub out the VC (integration layer) with the activatable
            V_HandheldActivatableStub v_handheldActivatableStub = new(handheldActivatable);

            //Get interfaces
            IV_HandheldActivatable handheldActivatablePluginInterface = v_handheldActivatableStub;
            IHandheldClickInteractionModule handheldActivatablePlayerInterface = handheldActivatable.HandheldClickInteractionModule;

            FreeGrabbableService freeGrabbable = new(
                new List<IHandheldInteractionModule>() { handheldActivatablePlayerInterface },
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
            handheldActivatablePluginInterface.OnActivate.AddListener(pluginScriptMock.HandleActivateReceived);
            handheldActivatablePluginInterface.OnDeactivate.AddListener(pluginScriptMock.HandleDeactivateReceived);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            Assert.IsTrue(grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(grabbablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID);

            //Invoke Activate, Check customer received the activate, and that the interactorID is set
            PlayerInputContainerSetup.HandheldClick2D.OnPressed += Raise.Event<Action>();
            pluginScriptMock.Received(1).HandleActivateReceived();
            Assert.IsTrue(handheldActivatablePluginInterface.IsActivated);
            Assert.AreEqual(handheldActivatablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID);

            //Invoke Deactivate, Check customer received the deactivate, and that the interactorID is set
            PlayerInputContainerSetup.HandheldClick2D.OnPressed += Raise.Event<Action>();
            pluginScriptMock.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(handheldActivatablePluginInterface.IsActivated);
            Assert.AreEqual(handheldActivatablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID);
        }
    }
}
