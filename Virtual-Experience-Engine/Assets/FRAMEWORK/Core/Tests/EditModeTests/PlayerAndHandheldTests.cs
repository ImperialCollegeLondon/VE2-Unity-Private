using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common;
using VE2.Core.Player;
using VE2.Core.VComponents.InteractableFindables;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.PluginInterfaces;
using VE2.Core.VComponents.Tests;
using static VE2.Common.CommonSerializables;

namespace VE2.Core.Tests
{
    public class PlayerAndHandheldTest
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
            PluginScriptMock pluginScriptMock = Substitute.For<PluginScriptMock>();
            handheldActivatablePluginInterface.OnActivate.AddListener(pluginScriptMock.HandleActivateReceived);
            handheldActivatablePluginInterface.OnDeactivate.AddListener(pluginScriptMock.HandleDeactivateReceived);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            inputHandlerStub.OnMouseLeftClick += Raise.Event<Action>();
            Assert.IsTrue(grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(grabbablePluginInterface.MostRecentInteractingClientID, localClientID);

            //Invoke Activate, Check customer received the activate, and that the interactorID is set
            inputHandlerStub.OnKeyboardActionKeyPressed += Raise.Event<Action>();
            pluginScriptMock.Received(1).HandleActivateReceived();
            Assert.IsTrue(handheldActivatablePluginInterface.IsActivated);
            Assert.AreEqual(handheldActivatablePluginInterface.MostRecentInteractingClientID, localClientID);

            //Invoke Deactivate, Check customer received the deactivate, and that the interactorID is set
            inputHandlerStub.OnKeyboardActionKeyPressed += Raise.Event<Action>();
            pluginScriptMock.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(handheldActivatablePluginInterface.IsActivated);
            Assert.AreEqual(handheldActivatablePluginInterface.MostRecentInteractingClientID, localClientID);
        }

        [Test]
        public void OnUserScroll_WithHandheldAdjustable_CustomerScriptReceivesOnValueAdjusted()
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

            HandheldAdjustableConfig handheldAdjustableConfig = new();

            float startingValue = handheldAdjustableConfig.HandheldAdjustableServiceConfig.StartingValue;
            float increment = handheldAdjustableConfig.HandheldAdjustableServiceConfig.IncrementPerMouseWheelScroll;

            handheldAdjustableConfig.StateConfig.MaximumValue = random.Next(0, 100);
            handheldAdjustableConfig.StateConfig.MinimumValue = random.Next(-100, 0);


            HandheldAdjustableService handheldAdjustable = new(handheldAdjustableConfig, new AdjustableState(), "debug", Substitute.For<WorldStateModulesContainer>());

            V_HandheldAdjustableStub v_handheldAdjustableStub = new(handheldAdjustable);

            IV_HandheldAdjustable handheldAdjustablePluginInterface = v_handheldAdjustableStub;
            IHandheldScrollInteractionModule handheldAdjustablePlayerInterface = handheldAdjustable.HandheldScrollInteractionModule;

            FreeGrabbableService freeGrabbable = new(
            new List<IHandheldInteractionModule>() { handheldAdjustablePlayerInterface },
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
            PluginScriptMock pluginScriptMock = Substitute.For<PluginScriptMock>();
            handheldAdjustablePluginInterface.OnValueAdjusted.AddListener((value) => pluginScriptMock.HandleValueAdjusted(value));

            //Invoke grab, check customer received the grab, and that the interactorID is set
            inputHandlerStub.OnMouseLeftClick += Raise.Event<Action>();
            Assert.IsTrue(grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(grabbablePluginInterface.MostRecentInteractingClientID, localClientID);

            //Invoke scroll up, check customer received the scroll up, and that the value is correct
            inputHandlerStub.OnMouseScrollUp += Raise.Event<Action>();
            pluginScriptMock.Received(1).HandleValueAdjusted(startingValue + increment);
            Assert.IsTrue(handheldAdjustablePluginInterface.Value == startingValue + increment);
            Assert.AreEqual(handheldAdjustablePluginInterface.MostRecentInteractingClientID, localClientID);

            //Invoke scroll down, check customer received the scroll down, and that the value is correct
            inputHandlerStub.OnMouseScrollDown += Raise.Event<Action>();
            pluginScriptMock.Received(1).HandleValueAdjusted(startingValue);
            Assert.IsTrue(handheldAdjustablePluginInterface.Value == startingValue);
            Assert.AreEqual(handheldAdjustablePluginInterface.MostRecentInteractingClientID, localClientID);

            //Invoke scroll down, check customer received the scroll down, and that the value is correct
            inputHandlerStub.OnMouseScrollDown += Raise.Event<Action>();
            pluginScriptMock.Received(1).HandleValueAdjusted(startingValue - increment);
            Assert.IsTrue(handheldAdjustablePluginInterface.Value == startingValue - increment);
            Assert.AreEqual(handheldAdjustablePluginInterface.MostRecentInteractingClientID, localClientID);
        }
    }
}

