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
    public class PlayerAndHandheldActivatableTests
    {
        [Test]
        public void OnUserClick_WithHandheldActivatable_CustomerScriptReceivesOnActivate()
        {
            //Create an ID
            System.Random random = new();
            ushort localClientID = (ushort)random.Next(0, ushort.MaxValue);
            ILocalClientIDProvider localClientIDProviderStub = Substitute.For<ILocalClientIDProvider>();
            localClientIDProviderStub.IsClientIDReady.Returns(true);
            localClientIDProviderStub.LocalClientID.Returns(localClientID);

            InteractorID interactorID = new(localClientID, InteractorType.Mouse2D);
            IInteractor interactorStub = Substitute.For<IInteractor>();
            InteractorContainer interactorContainerStub = new();
            interactorContainerStub.RegisterInteractor(interactorID.ToString(), interactorStub);

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
                interactorContainerStub,
                Substitute.For<IRigidbodyWrapper>(),
                new PhysicsConstants());

            //Stub out the VC (integration layer) with the grabbable
            V_FreeGrabbableStub v_freeGrabbableStub = new(freeGrabbable);

            //Get interfaces
            IV_FreeGrabbable grabbablePluginInterface = v_freeGrabbableStub;
            IRangedGrabInteractionModuleProvider grabbableRaycastInterface = v_freeGrabbableStub;
            IRangedGrabInteractionModule grabbablePlayerInterface = grabbableRaycastInterface.RangedGrabInteractionModule;

            //Stub out the player settings provider with default settings
            IPlayerPersistentDataHandler playerSettingsProviderStub = Substitute.For<IPlayerPersistentDataHandler>();
            playerSettingsProviderStub.PlayerPresentationConfig.Returns(new PlayerPresentationConfig());

            //Stub out the input handler    
            PlayerInputContainerStubWrapper playerInputContainerStubWrapper = new();

            //Stub out the raycast provider to hit the activatable GO with 0 range
            IRaycastProvider raycastProviderStub = Substitute.For<IRaycastProvider>();
            raycastProviderStub
                .Raycast(default, default, default, default)
                .ReturnsForAnyArgs(new RaycastResultWrapper(grabbablePlayerInterface, null, 0));

                    //Create the player (2d)
            PlayerService playerService = new(
                new PlayerTransformData(),
                new PlayerConfig(),
                interactorContainerStub,
                Substitute.For<IPlayerPersistentDataHandler>(),
                localClientIDProviderStub,
                playerInputContainerStubWrapper.PlayerInputContainer,
                raycastProviderStub,
                Substitute.For<IXRManagerWrapper>()
            );

            //Wire up the customer script to receive the events
            PluginScriptMock pluginScriptMock = Substitute.For<PluginScriptMock>();
            handheldActivatablePluginInterface.OnActivate.AddListener(pluginScriptMock.HandleActivateReceived);
            handheldActivatablePluginInterface.OnDeactivate.AddListener(pluginScriptMock.HandleDeactivateReceived);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            playerInputContainerStubWrapper.Grab2D.OnPressed += Raise.Event<Action>();
            Assert.IsTrue(grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(grabbablePluginInterface.MostRecentInteractingClientID, localClientID);

            //Invoke Activate, Check customer received the activate, and that the interactorID is set
            playerInputContainerStubWrapper.HandheldClick2D.OnPressed += Raise.Event<Action>();
            pluginScriptMock.Received(1).HandleActivateReceived();
            Assert.IsTrue(handheldActivatablePluginInterface.IsActivated);
            Assert.AreEqual(handheldActivatablePluginInterface.MostRecentInteractingClientID, localClientID);

            //Invoke Deactivate, Check customer received the deactivate, and that the interactorID is set
            playerInputContainerStubWrapper.HandheldClick2D.OnPressed += Raise.Event<Action>();
            pluginScriptMock.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(handheldActivatablePluginInterface.IsActivated);
            Assert.AreEqual(handheldActivatablePluginInterface.MostRecentInteractingClientID, localClientID);
        }
    }
}
