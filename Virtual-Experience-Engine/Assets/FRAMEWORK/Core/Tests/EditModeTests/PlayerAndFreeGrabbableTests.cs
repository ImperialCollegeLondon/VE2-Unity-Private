using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using VE2.Core.VComponents.Internal;
using System;
using VE2.Core.VComponents.Tests;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;
using VE2.Core.Player.Internal;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Tests
{
    public class PlayerAndFreeGrabbableTests
    {
        [Test]
        public void OnUserGrab_WithHoveringGrabbable_CustomerScriptReceivesOnGrab()
        {
            //Create an ID
            System.Random random = new();
            ushort localClientID = (ushort)random.Next(0, ushort.MaxValue);
            ILocalClientIDProvider localClientProviderStub = Substitute.For<ILocalClientIDProvider>();
            localClientProviderStub.IsClientIDReady.Returns(true);
            localClientProviderStub.LocalClientID.Returns(localClientID);
            
            InteractorID interactorID = new(localClientID, InteractorType.Mouse2D);
            IInteractor interactorStub = Substitute.For<IInteractor>();
            InteractorContainer interactorContainerStub = new();
            interactorContainerStub.RegisterInteractor(interactorID.ToString(), interactorStub);

            FreeGrabbableService freeGrabbable = new( 
                new List<IHandheldInteractionModule>() {},
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
            localClientProviderStub,
            playerInputContainerStubWrapper.PlayerInputContainer,
            raycastProviderStub,
            Substitute.For<IXRManagerWrapper>()
        );

            //Wire up the customer script to receive the events
            PluginGrabbableScript pluginScript = Substitute.For<PluginGrabbableScript>();
            grabbablePluginInterface.OnGrab.AddListener(pluginScript.HandleGrabReceived);
            grabbablePluginInterface.OnDrop.AddListener(pluginScript.HandleDropReceived);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            playerInputContainerStubWrapper.Grab2D.OnPressed += Raise.Event<Action>();
            pluginScript.Received(1).HandleGrabReceived();
            Assert.IsTrue(grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(grabbablePluginInterface.MostRecentInteractingClientID, localClientID);

            //Invoke drop, Check customer received the drop, and that the interactorID is set
            playerInputContainerStubWrapper.Grab2D.OnPressed += Raise.Event<Action>();
            pluginScript.Received(1).HandleDropReceived();
            Assert.IsFalse(grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(grabbablePluginInterface.MostRecentInteractingClientID, localClientID);
        }
    }
}
