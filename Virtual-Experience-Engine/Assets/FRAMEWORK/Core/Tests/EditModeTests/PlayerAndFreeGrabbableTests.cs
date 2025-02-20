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
    [TestFixture]
    internal class PlayerAndFreeGrabbableTests  : PlayerServiceSetupFixture
    {
        [Test]
        public void OnUserGrab_WithHoveringGrabbable_CustomerScriptReceivesOnGrab()
        {
            FreeGrabbableService freeGrabbable = new( 
                new List<IHandheldInteractionModule>() {},
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
            PluginGrabbableScript pluginScript = Substitute.For<PluginGrabbableScript>();
            grabbablePluginInterface.OnGrab.AddListener(pluginScript.HandleGrabReceived);
            grabbablePluginInterface.OnDrop.AddListener(pluginScript.HandleDropReceived);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            pluginScript.Received(1).HandleGrabReceived();
            Assert.IsTrue(grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(grabbablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID);

            //Invoke drop, Check customer received the drop, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            pluginScript.Received(1).HandleDropReceived();
            Assert.IsFalse(grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(grabbablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID);
        }
    }
}
