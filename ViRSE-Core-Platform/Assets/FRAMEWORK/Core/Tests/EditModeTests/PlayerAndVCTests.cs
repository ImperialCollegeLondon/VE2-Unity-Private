using NSubstitute;
using NUnit.Framework;
using ViRSE.Core.Shared;
using ViRSE.Core;
using ViRSE.Core.VComponents;
using ViRSE.Core.Player;
using static ViRSE.Core.Shared.CoreCommonSerializables;
using System;


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

            //Stub out the player settings provider with default settings
            IPlayerSettingsProvider playerSettingsProviderStub = Substitute.For<IPlayerSettingsProvider>();
            playerSettingsProviderStub.UserSettings.Returns(new UserSettingsPersistable());

            //Stub out the multiplayer support
            IMultiplayerSupport multiplayerSupportStub = Substitute.For<IMultiplayerSupport>();
            multiplayerSupportStub.IsConnectedToServer.Returns(true);
            multiplayerSupportStub.LocalClientID.Returns((ushort)50);

            //Stub out the input handler    
            IInputHandler inputHandlerStub = Substitute.For<IInputHandler>();

            //Stub out the raycast provider to hit the activatable with 0 range
            IRangedClickPlayerInteractableImplementor pushActivatablePlayerInterface = pushActivatable;
            IRaycastProvider raycastProviderStub = Substitute.For<IRaycastProvider>();
            raycastProviderStub
                .TryGetRangedPlayerInteractable(default, default, out Arg.Any<RangedPlayerInteractableHitResult>(), default, default)
                .ReturnsForAnyArgs(x =>
                {
                    x[2] = new RangedPlayerInteractableHitResult(pushActivatablePlayerInterface.RangedPlayerInteractable, 0);
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
            IPushActivatable pushActivatableCustomerInterface = pushActivatable;
            CustomerScriptMock customerScriptMock = Substitute.For<CustomerScriptMock>();
            pushActivatableCustomerInterface.OnActivate.AddListener(customerScriptMock.HandleActivateReceived);
            pushActivatableCustomerInterface.OnDeactivate.AddListener(customerScriptMock.HandleDeactivateReceived);

            //Check customer received the activation, and that the interactorID is set
            inputHandlerStub.OnMouseLeftClick += Raise.Event<Action>();
            customerScriptMock.Received(100).HandleActivateReceived();
            Assert.IsTrue(pushActivatableCustomerInterface.IsActivated);
            InteractorID interactorID = pushActivatableCustomerInterface.CurrentInteractor;
            Assert.AreEqual(50, interactorID.ClientID);
            Assert.AreEqual(InteractorType.TwoD, interactorID.InteractorType);

            // Invoke the click to deactivate
            inputHandlerStub.OnMouseLeftClick += Raise.Event<Action>();
            customerScriptMock.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(pushActivatableCustomerInterface.IsActivated);
            Assert.IsNull(pushActivatableCustomerInterface.CurrentInteractor);
        }

        public class CustomerScriptMock
        {
            public void HandleActivateReceived() { }

            public void HandleDeactivateReceived() { }
        }
    }
}
