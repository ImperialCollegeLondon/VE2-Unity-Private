using NSubstitute;
using NUnit.Framework;
using ViRSE.Core.Shared;
using ViRSE.Core;
using ViRSE.Core.VComponents;
using ViRSE.Core.Player;
using static ViRSE.Core.Shared.CoreCommonSerializables;
using System;
using UnityEngine.XR.Management;


namespace ViRSE.Tests
{
    public class PushActivatableTests
    {
        [Test]
        public void OnUserClick_WithHoveringActivatable_CustomerScriptReceivesOnActivate()
        {
            //Create an activatable with default config and state
            PushActivatable pushActivatable = new( 
                new PushActivatableConfig(),
                new SingleInteractorActivatableState(),
                "testID",
                Substitute.For<WorldStateModulesContainer>()
            );

            //Wire up mock customer script
            IPushActivatable pushActivatableInterface = pushActivatable;
            CustomerScriptMock customerScriptMock = new();;
            pushActivatableInterface.OnActivate.AddListener(customerScriptMock.HandleActivateReceived);
            pushActivatableInterface.OnDeactivate.AddListener(customerScriptMock.HandleDeactivateReceived);

            //Stub out the player settings provider with default settings
            IPlayerSettingsProvider playerSettingsProviderStub = Substitute.For<IPlayerSettingsProvider>();
            playerSettingsProviderStub.UserSettings.Returns(new UserSettingsPersistable());

            //Stub out the multiplayer support
            IMultiplayerSupport multiplayerSupportStub = Substitute.For<IMultiplayerSupport>();
            multiplayerSupportStub.IsConnectedToServer.Returns(true);
            multiplayerSupportStub.LocalClientID.Returns((ushort)50);

            //Stub out the input handler    
            IInputHandler inputHandlerStub = Substitute.For<IInputHandler>();

            //Stub out the raycast provider to hit the activatable
            IRaycastProvider raycastProviderStub = Substitute.For<IRaycastProvider>();
            raycastProviderStub
                .TryGetRangedPlayerInteractable(default, default, out Arg.Any<IRangedPlayerInteractableImplementor>(), default, default)
                .ReturnsForAnyArgs(x =>
                {
                    x[2] = pushActivatable;
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
                Substitute.For<XRManagerSettings>()
            );

            //Check customer received the activation, and that the interactorID is set
            inputHandlerStub.OnMouseLeftClick += Raise.Event<Action>();
            customerScriptMock.DidNotReceive().HandleActivateReceived();
            customerScriptMock.Received(1).HandleActivateReceived();
            customerScriptMock.Received(1).HandleDeactivateReceived(); //Wrong, not actually received deactivate yet
            customerScriptMock.Received(0).HandleActivateReceived(); //Wrong, there has been 1 activate
            customerScriptMock.Received(100).HandleActivateReceived(); //Wrong again!
            Assert.IsTrue(pushActivatableInterface.IsActivated);
            InteractorID interactorID = pushActivatableInterface.CurrentInteractor;
            Assert.AreEqual(50, interactorID.ClientID);
            Assert.AreEqual(InteractorType.TwoD, interactorID.InteractorType);

            // Invoke the click to deactivate
            inputHandlerStub.OnMouseLeftClick += Raise.Event<Action>();
            customerScriptMock.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(pushActivatableInterface.IsActivated);
            Assert.IsNull(pushActivatableInterface.CurrentInteractor);
        }

        public class CustomerScriptMock
        {
            public void HandleActivateReceived() { }

            public void HandleDeactivateReceived() { }
        }
    }
}
