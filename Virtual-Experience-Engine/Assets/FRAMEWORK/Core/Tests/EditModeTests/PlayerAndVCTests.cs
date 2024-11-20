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
using VE2.Core.Common;

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
            IPressableInput rangedClickInputStub = Substitute.For<IPressableInput>();
            PlayerInputContainer playerInputContainerStub = PlayerInputContainerStubFactory.Create(rangedClick2D: rangedClickInputStub); 

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
                playerInputContainerStub,
                raycastProviderStub, 
                Substitute.For<IXRManagerWrapper>()
            );

            //Wire up mock customer script
            PluginScriptMock PluginScriptMock = Substitute.For<PluginScriptMock>();
            activatablePluginInterface.OnActivate.AddListener(PluginScriptMock.HandleActivateReceived);
            activatablePluginInterface.OnDeactivate.AddListener(PluginScriptMock.HandleDeactivateReceived);

            //Check customer received the activation, and that the interactorID is set
            rangedClickInputStub.OnPressed += Raise.Event<Action>();
            PluginScriptMock.Received(1).HandleActivateReceived(); 
            Assert.IsTrue(activatablePluginInterface.IsActivated);
            Assert.AreEqual(activatablePluginInterface.MostRecentInteractingClientID, localClientID);

            // Invoke the click to deactivate
            rangedClickInputStub.OnPressed += Raise.Event<Action>();
            PluginScriptMock.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(activatablePluginInterface.IsActivated);
            Assert.AreEqual(activatablePluginInterface.MostRecentInteractingClientID, localClientID);
        }
    }

    public static class PlayerInputContainerStubFactory
    {
        public static PlayerInputContainer Create(
            IPressableInput changeMode2D = null,
            IPressableInput inspectModeButton = null, IPressableInput rangedClick2D = null, IPressableInput grab2D = null, IPressableInput handheldClick2D = null, IScrollInput scrollTickUp2D = null, IScrollInput scrollTickDown2D = null,
            IPressableInput resetViewVR = null,
            IValueInput<Vector3> handVRLeftPosition = null, IValueInput<Quaternion> handVRLeftRotation = default,
            IPressableInput rangedClickVRLeft = null, IPressableInput grabVRLeft = null, IPressableInput handheldClickVRLeft = null, IScrollInput scrollTickUpVRLeft = null, IScrollInput scrollTickDownVRLeft = null,
            IValueInput<Vector3> handVRRightPosition = null, IValueInput<Quaternion> handVRRightRotation = default,
            IPressableInput rangedClickVRRight = null, IPressableInput grabVRRight = null, IPressableInput handheldClickVRRight = null, IScrollInput scrollTickUpVRRight = null, IScrollInput scrollTickDownVRRight = null)
        {
            changeMode2D ??= Substitute.For<IPressableInput>();

            //2D player
            inspectModeButton ??= Substitute.For<IPressableInput>();
            rangedClick2D ??= Substitute.For<IPressableInput>();
            grab2D ??= Substitute.For<IPressableInput>();
            handheldClick2D ??= Substitute.For<IPressableInput>();
            scrollTickUp2D ??= Substitute.For<IScrollInput>();
            scrollTickDown2D ??= Substitute.For<IScrollInput>();
            resetViewVR ??= Substitute.For<IPressableInput>();

            //Left hand VR
            handVRLeftPosition ??= Substitute.For<IValueInput<Vector3>>();
            handVRLeftRotation ??= Substitute.For<IValueInput<Quaternion>>();
            rangedClickVRLeft ??= Substitute.For<IPressableInput>();
            grabVRLeft ??= Substitute.For<IPressableInput>();
            handheldClickVRLeft ??= Substitute.For<IPressableInput>();
            scrollTickUpVRLeft ??= Substitute.For<IScrollInput>();
            scrollTickDownVRLeft ??= Substitute.For<IScrollInput>();

            //Right hand VR
            handVRRightPosition ??= Substitute.For<IValueInput<Vector3>>();
            handVRRightRotation ??= Substitute.For<IValueInput<Quaternion>>();
            rangedClickVRRight ??= Substitute.For<IPressableInput>();
            grabVRRight ??= Substitute.For<IPressableInput>();
            handheldClickVRRight ??= Substitute.For<IPressableInput>();
            scrollTickUpVRRight ??= Substitute.For<IScrollInput>();
            scrollTickDownVRRight ??= Substitute.For<IScrollInput>();

            // Return a PlayerInputContainer with all inputs set
            return new PlayerInputContainer(
                changeMode2D: changeMode2D,
                inspectModeButton: inspectModeButton,
                rangedClick2D: rangedClick2D,
                grab2D: grab2D,
                handheldClick2D: handheldClick2D,
                scrollTickUp2D: scrollTickUp2D,
                scrollTickDown2D: scrollTickDown2D,
                resetViewVR: resetViewVR,
                handVRLeftPosition: handVRLeftPosition,
                handVRLeftRotation: handVRLeftRotation,
                rangedClickVRLeft: rangedClickVRLeft,
                grabVRLeft: grabVRLeft,
                handheldClickVRLeft: handheldClickVRLeft,
                scrollTickUpVRLeft: scrollTickUpVRLeft,
                scrollTickDownVRLeft: scrollTickDownVRLeft,
                handVRRightPosition: handVRRightPosition,
                handVRRightRotation: handVRRightRotation,
                rangedClickVRRight: rangedClickVRRight,
                grabVRRight: grabVRRight,
                handheldClickVRRight: handheldClickVRRight,
                scrollTickUpVRRight: scrollTickUpVRRight,
                scrollTickDownVRRight: scrollTickDownVRRight
            );
        }
    }
}
