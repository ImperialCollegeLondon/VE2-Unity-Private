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
    public class PlayerAndHandheldActivatableTests
    {
        [Test]
        public void OnUserClick_WithHandheldActivatable_CustomerScriptReceivesOnActivate()
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

            //Create the activatable with default values
            HandheldActivatableService handheldActivatable = new(new HandheldActivatableConfig(), new SingleInteractorActivatableState(), "debug", Substitute.For<WorldStateModulesContainer>());

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
                Substitute.For<WorldStateModulesContainer>(),
                findProviderStub,
                Substitute.For<IRigidbodyWrapper>(),
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
            PlayerInputContainerStubWrapper playerInputContainerStubWrapper = new();

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
