using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using VE2;
using VE2.Common;
using VE2.Core.Player;
using VE2.Core.Tests;
using VE2.Core.VComponents.InteractableFindables;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.PluginInterfaces;
using VE2.Core.VComponents.Tests;
using static VE2.Common.CommonSerializables;

public class PlayerAndHandheldAdjustableTests
{
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
        IInteractor interactorStub = Substitute.For<IInteractor>();
        InteractorContainer interactorContainerStub = new();
        interactorContainerStub.RegisterInteractor(interactorID.ToString(), interactorStub);

        HandheldAdjustableConfig handheldAdjustableConfig = new();

        float startingValue = handheldAdjustableConfig.StateConfig.StartingValue;
        float increment = handheldAdjustableConfig.HandheldAdjustableServiceConfig.IncrementPerScrollTick;

        handheldAdjustableConfig.StateConfig.MaximumOutputValue = random.Next(0, 100);
        handheldAdjustableConfig.StateConfig.MinimumOutputValue = random.Next(-100, 0);

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
        interactorContainerStub,
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
            .Raycast(default, default, default, default)
            .ReturnsForAnyArgs(new RaycastResultWrapper(grabbablePlayerInterface, 0));

        //Create the player (2d)
        PlayerService playerService = new(
            new PlayerTransformData(),
            new PlayerStateConfig(),
            false,
            true,
            new PlayerStateModuleContainer(),
            interactorContainerStub,
            playerSettingsProviderStub,
            Substitute.For<IPlayerAppearanceOverridesProvider>(),
            multiplayerSupportStub,
            playerInputContainerStubWrapper.PlayerInputContainer,
            raycastProviderStub,
            Substitute.For<IXRManagerWrapper>()
        );

        //Wire up the customer script to receive the events
        PluginScriptMock pluginScriptMock = Substitute.For<PluginScriptMock>();
        handheldAdjustablePluginInterface.OnValueAdjusted.AddListener((value) => pluginScriptMock.HandleValueAdjusted(value));

        //Invoke grab, check customer received the grab, and that the interactorID is set
        playerInputContainerStubWrapper.Grab2D.OnPressed += Raise.Event<Action>();
        Assert.IsTrue(grabbablePluginInterface.IsGrabbed);
        Assert.AreEqual(grabbablePluginInterface.MostRecentInteractingClientID, localClientID);

        //Invoke scroll up, check customer received the scroll up, and that the value is correct
        playerInputContainerStubWrapper.ScrollTickUp2D.OnTickOver += Raise.Event<Action>();
        pluginScriptMock.Received(1).HandleValueAdjusted(startingValue + increment);
        Assert.IsTrue(handheldAdjustablePluginInterface.Value == startingValue + increment);
        Assert.AreEqual(handheldAdjustablePluginInterface.MostRecentInteractingClientID, localClientID);

        //Invoke scroll down, check customer received the scroll down, and that the value is correct
        playerInputContainerStubWrapper.ScrollTickDown2D.OnTickOver += Raise.Event<Action>();
        pluginScriptMock.Received(1).HandleValueAdjusted(startingValue);
        Assert.IsTrue(handheldAdjustablePluginInterface.Value == startingValue);
        Assert.AreEqual(handheldAdjustablePluginInterface.MostRecentInteractingClientID, localClientID);

        //Invoke scroll down, check customer received the scroll down, and that the value is correct
        playerInputContainerStubWrapper.ScrollTickDown2D.OnTickOver += Raise.Event<Action>();
        pluginScriptMock.Received(1).HandleValueAdjusted(startingValue - increment);
        Assert.IsTrue(handheldAdjustablePluginInterface.Value == startingValue - increment);
        Assert.AreEqual(handheldAdjustablePluginInterface.MostRecentInteractingClientID, localClientID);
    }
}
