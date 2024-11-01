using NSubstitute;
using NUnit.Framework;
using ViRSE.Core.VComponents;
using ViRSE.Core.Player;
using System;
using ViRSE.Core.VComponents.Tests;
using ViRSE.Core.VComponents.PlayerInterfaces;
using ViRSE.Core.VComponents.PluginInterfaces;
using UnityEngine;
using ViRSE.Common;
using static ViRSE.Common.CoreCommonSerializables;
using VIRSE.Common;


namespace ViRSE.Tests
{
    public class PushActivatableTests
    {
        [Test]
        public void OnUserClick_WithHoveringActivatable_CustomerScriptReceivesOnActivate()
        {
            //Create an activatable with default config and state
            ToggleActivatable toggleActivatable = new( 
                new ToggleActivatableConfig(),
                new SingleInteractorActivatableState(),
                "testID",
                Substitute.For<WorldStateModulesContainer>()
            );

            //Stub out the VC (integration layer) with the activatable
            GameObject gameObjectHitStub = new();
            V_ToggleActivatableStub v_activatableStub = gameObjectHitStub.AddComponent<V_ToggleActivatableStub>();
            v_activatableStub.ToggleActivatable = toggleActivatable;

            //Get interfaces
            IV_ToggleActivatable activatablePluginInterface = v_activatableStub;
            IRangedClickPlayerInteractable activatablePlayerInterface = v_activatableStub;

            //Stub out the player settings provider with default settings
            IPlayerSettingsProvider playerSettingsProviderStub = Substitute.For<IPlayerSettingsProvider>();
            playerSettingsProviderStub.UserSettings.Returns(new UserSettingsPersistable());

            //Stub out the multiplayer support
            System.Random random = new();
            ushort localClientID = (ushort)random.Next(0, ushort.MaxValue + 1);
            IMultiplayerSupport multiplayerSupportStub = Substitute.For<IMultiplayerSupport>();
            multiplayerSupportStub.IsConnectedToServer.Returns(true);
            multiplayerSupportStub.LocalClientID.Returns(localClientID);

            //Stub out the input handler    
            IInputHandler inputHandlerStub = Substitute.For<IInputHandler>();

            //Stub out the raycast provider to hit the activatable GO with 0 range
            IRaycastProvider raycastProviderStub = Substitute.For<IRaycastProvider>();
            raycastProviderStub
                .TryGetGameObject(default, default, out Arg.Any<RaycastResultWrapper>(), default, default)
                .ReturnsForAnyArgs(x =>
                {
                    x[2] = new RaycastResultWrapper(gameObjectHitStub, 0);
                    return true;
                });

            //Create the player (2d)
            ViRSEPlayerService playerService = new(
                new PlayerTransformData(),
                new PlayerStateConfig(),
                false,
                true,
                Substitute.For<ViRSEPlayerStateModuleContainer>(),
                playerSettingsProviderStub,
                Substitute.For<IPlayerAppearanceOverridesProvider>(),
                multiplayerSupportStub,
                inputHandlerStub,
                raycastProviderStub, 
                Substitute.For<IXRManagerWrapper>()
            );

            //Wire up mock customer script
            PluginScriptMock PluginScriptMock = Substitute.For<PluginScriptMock>();
            PluginScriptMock.HandleActivateReceived(); //This is fine 
            activatablePluginInterface.OnActivate.AddListener(PluginScriptMock.HandleActivateReceived); //But a null ref here??
            activatablePluginInterface.OnDeactivate.AddListener(PluginScriptMock.HandleDeactivateReceived);

            //Check customer received the activation, and that the interactorID is set
            inputHandlerStub.OnMouseLeftClick += Raise.Event<Action>();
            PluginScriptMock.Received(100).HandleActivateReceived();
            Assert.IsTrue(activatablePluginInterface.IsActivated);
            Assert.AreEqual(activatablePluginInterface.MostRecentInteractingClientID, localClientID);

            // Invoke the click to deactivate
            inputHandlerStub.OnMouseLeftClick += Raise.Event<Action>();
            PluginScriptMock.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(activatablePluginInterface.IsActivated);
            Assert.AreEqual(activatablePluginInterface.MostRecentInteractingClientID, localClientID);
        }

        public class PluginScriptMock
        {
            public void HandleActivateReceived() { }

            public void HandleDeactivateReceived() { }
        }
    }
}
