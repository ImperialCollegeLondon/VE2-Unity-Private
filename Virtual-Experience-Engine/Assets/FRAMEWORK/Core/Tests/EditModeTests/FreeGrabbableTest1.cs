using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using VE2.Core.VComponents.PluginInterfaces;
using UnityEngine;
using VE2.Common;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.NonInteractableInterfaces;
using VE2.Core.VComponents.InteractableFindables;
using VE2.Core.VComponents.Internal;
using static VE2.Common.CommonSerializables;
using VE2.Core.Player;
using System;

namespace VE2.Core.VComponents.Tests
{
    public class PlayerAndFreeGrabbableTests
    {
        [Test]
        public void OnUserGrab_WithHoveringGrabbable_CustomerScriptReceivesOnGrab()
        {
            //Create an ID
            System.Random random = new();
            ushort localClientID = (ushort)random.Next(0, ushort.MaxValue);
            IMultiplayerSupport multiplayerSupportStub = Substitute.For<IMultiplayerSupport>();
            multiplayerSupportStub.IsConnectedToServer.Returns(true);
            multiplayerSupportStub.LocalClientID.Returns(localClientID);
            
            InteractorID interactorID = new(localClientID, InteractorType.Mouse2D);
            string interactorGameobjectName = $"Interactor{interactorID.ClientID}-{interactorID.InteractorType}";

            IInteractor interactorStub = Substitute.For<IInteractor>();
            GameObject interactorGameObject = new();

            IGameObjectFindProvider findProviderStub = Substitute.For<IGameObjectFindProvider>();
            findProviderStub.FindGameObject(interactorGameobjectName).Returns(interactorGameObject);
            findProviderStub.TryGetComponent<IInteractor>(interactorGameObject, out Arg.Any<IInteractor>()).Returns(x =>
            {
                x[1] = interactorStub;
                return true;
            });

            FreeGrabbableService freeGrabbable = new( 
                new List<IHandheldInteractionModule>() {},
                new FreeGrabbableConfig(),
                new FreeGrabbableState(), 
                "debug",
                Substitute.For<WorldStateModulesContainer>(),
                findProviderStub,
                new Rigidbody(), 
                new PhysicsConstants());

            //Stub out the VC (integration layer) with the grabbable
            V_FreeGrabbableStub v_freeGrabbableStub = new(freeGrabbable);

            //Get interfaces
            IV_FreeGrabbable grabbablePluginInterface = v_freeGrabbableStub;
            IRangedGrabPlayerInteractableIntegrator grabbableRaycastInterface = v_freeGrabbableStub;
            IRangedGrabInteractionModule grabbablePlayerInterface = grabbableRaycastInterface.RangedGrabInteractionModule;

            //Stub out the player settings provider with default settings
            IPlayerSettingsProvider playerSettingsProviderStub = Substitute.For<IPlayerSettingsProvider>();
            playerSettingsProviderStub.UserSettings.Returns(new UserSettingsPersistable());

            //Stub out the input handler    
            IInputHandler inputHandlerStub = Substitute.For<IInputHandler>();

            //Stub out the raycast provider to hit the activatable GO with 0 range
            IRaycastProvider raycastProviderStub = Substitute.For<IRaycastProvider>();
            raycastProviderStub
                .TryGetRangedInteractionModule(default, default, out Arg.Any<RaycastResultWrapper>(), default, default)
                .ReturnsForAnyArgs(x =>
                {
                    x[2] = new RaycastResultWrapper(grabbableRaycastInterface.RangedGrabInteractionModule, 0);
                    return true;
                });

            //Create the player (2d)
            PlayerService playerService = new(
                new PlayerTransformData(),
                new PlayerStateConfig(),
                false,
                true,
                new PlayerStateModuleContainer(),
                playerSettingsProviderStub,
                Substitute.For<IPlayerAppearanceOverridesProvider>(),
                multiplayerSupportStub,
                inputHandlerStub,
                raycastProviderStub, 
                Substitute.For<IXRManagerWrapper>()
            );

            //Wire up the customer script to receive the events
            PluginGrabbableScript pluginScript = Substitute.For<PluginGrabbableScript>();
            grabbablePluginInterface.OnGrab.AddListener(pluginScript.HandleGrabReceived);
            grabbablePluginInterface.OnDrop.AddListener(pluginScript.HandleDropReceived);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            inputHandlerStub.OnMouseLeftClick += Raise.Event<Action>();
            pluginScript.Received(1).HandleGrabReceived();
            Assert.IsTrue(grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(grabbablePluginInterface.MostRecentInteractingClientID, localClientID);

            //Invoke drop, Check customer received the drop, and that the interactorID is set
            inputHandlerStub.OnMouseLeftClick += Raise.Event<Action>();
            pluginScript.Received(1).HandleDropReceived();
            Assert.IsFalse(grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(grabbablePluginInterface.MostRecentInteractingClientID, localClientID);
        }
    }
}
