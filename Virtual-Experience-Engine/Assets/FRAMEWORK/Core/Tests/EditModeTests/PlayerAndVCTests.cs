using NSubstitute;
using NUnit.Framework;
using VE2.Core.Player;
using System;
using VE2.Core.VComponents.Tests;
using VE2.Core.VComponents.PluginInterfaces;
using UnityEngine;
using VE2.Common;
using static VE2.Common.CommonSerializables;
using VE2.Core.VComponents.InteractableFindables;
using VE2.Core.VComponents.Internal;

namespace VE2.Core.Tests
{
    public class PushActivatableTests
    {
        [Test]
        public void OnUserClick_WithHoveringActivatable_CustomerScriptReceivesOnActivate()
        {
            //Create an activatable with default config and state
            ToggleActivatableService toggleActivatable = new( 
                new ToggleActivatableConfig(),
                new SingleInteractorActivatableState(),
                "testID",
                new WorldStateModulesContainer()
            );

            //Stub out the VC (integration layer) with the activatable
            V_ToggleActivatableStub v_activatableStub = new(toggleActivatable);

            //Get interfaces
            IV_ToggleActivatable activatablePluginInterface = v_activatableStub;
            IRangedClickPlayerInteractableIntegrator activatableRaycastInterface = v_activatableStub;

            //Stub out the player settings provider with default settings
            IPlayerSettingsProvider playerSettingsProviderStub = Substitute.For<IPlayerSettingsProvider>();
            playerSettingsProviderStub.UserSettings.Returns(new UserSettingsPersistable());

            //Stub out the multiplayer support
            System.Random random = new();
            ushort localClientID = (ushort)random.Next(0, ushort.MaxValue);
            IMultiplayerSupport multiplayerSupportStub = Substitute.For<IMultiplayerSupport>();
            multiplayerSupportStub.IsConnectedToServer.Returns(true);
            multiplayerSupportStub.LocalClientID.Returns(localClientID);

            //Stub out the input handler    
            IInputHandler inputHandlerStub = Substitute.For<IInputHandler>();

            //Stub out the raycast provider to hit the activatable GO with 0 range
            IRaycastProvider raycastProviderStub = Substitute.For<IRaycastProvider>();
            raycastProviderStub
                .TryGetRangedInteractionModule(default, default, out Arg.Any<RaycastResultWrapper>(), default, default)
                .ReturnsForAnyArgs(x =>
                {
                    x[2] = new RaycastResultWrapper(activatableRaycastInterface.RangedClickInteractionModule, 0);
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

            //Wire up mock customer script
            PluginScriptMock PluginScriptMock = Substitute.For<PluginScriptMock>();
            activatablePluginInterface.OnActivate.AddListener(PluginScriptMock.HandleActivateReceived);
            activatablePluginInterface.OnDeactivate.AddListener(PluginScriptMock.HandleDeactivateReceived);

            //Check customer received the activation, and that the interactorID is set
            inputHandlerStub.OnMouseLeftClick += Raise.Event<Action>();
            PluginScriptMock.Received(1).HandleActivateReceived(); 
            Assert.IsTrue(activatablePluginInterface.IsActivated);
            Assert.AreEqual(activatablePluginInterface.MostRecentInteractingClientID, localClientID);

            // Invoke the click to deactivate
            inputHandlerStub.OnMouseLeftClick += Raise.Event<Action>();
            PluginScriptMock.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(activatablePluginInterface.IsActivated);
            Assert.AreEqual(activatablePluginInterface.MostRecentInteractingClientID, localClientID);
        }
    }
}
