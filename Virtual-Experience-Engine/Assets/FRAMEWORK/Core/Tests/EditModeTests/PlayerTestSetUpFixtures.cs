using NUnit.Framework;
using NSubstitute;
using VE2.Common;
using static VE2.Common.CommonSerializables;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.Player;
using UnityEngine;
using VE2.Core.Common;

namespace VE2.Core.Tests
{
    [SetUpFixture]
    public class CoreTestSetUp 
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            PlayerSettingsProviderSetup.PlayerSettingsProviderStubSetup();
            InputHandlerSetup.InputHandlerStubSetup();
            MultiplayerSupportSetup.MultiplayerSupportStubSetup();
            InteractorSetup.InteractorStubSetup();
            RayCastProviderSetup.RayCastProviderStubSetup();
        }
    }

    public class PlayerSettingsProviderSetup
    {
        public static IPlayerSettingsProvider PlayerSettingsProviderStub { get; private set; }

        public static void PlayerSettingsProviderStubSetup()
        {
            //Stub out the player settings provider with default settings
            PlayerSettingsProviderStub = Substitute.For<IPlayerSettingsProvider>();
            PlayerSettingsProviderStub.UserSettings.Returns(new UserSettingsPersistable());
        }

        public static void StubUserSettingsValueForPlayerSettingsProviderStub(UserSettingsPersistable userSettings)
        {
            PlayerSettingsProviderStub.UserSettings.Returns(userSettings);
        }
    }

    public class InputHandlerSetup
    {
        public static PlayerInputContainerStubWrapper PlayerInputContainerStubWrapper { get; private set; }

        public static void InputHandlerStubSetup()
        {
            //Stub out the input handler    
            PlayerInputContainerStubWrapper = new();
        }
    }
    
    public class MultiplayerSupportSetup
    {
        public static IMultiplayerSupport MultiplayerSupportStub { get; private set; }
        public static ushort LocalClientID { get; private set; }

        public static void MultiplayerSupportStubSetup()
        {
            //Stub out the multiplayer support
            System.Random random = new();
            LocalClientID = (ushort)random.Next(0, ushort.MaxValue);

            MultiplayerSupportStub = Substitute.For<IMultiplayerSupport>();
            MultiplayerSupportStub.IsConnectedToServer.Returns(true);
            StubLocalClientIDForMultiplayerSupportStub(LocalClientID);
        }

        public static void StubLocalClientIDForMultiplayerSupportStub(ushort localClientID)
        {
            MultiplayerSupportStub.LocalClientID.Returns(localClientID);
        }
    }

    public class InteractorSetup
    {
        public static InteractorContainer InteractorContainerStub { get; private set; }

        public static void InteractorStubSetup()
        {
            InteractorContainerStub = new();
        }

        public static void RegisterInteractor(InteractorID interactorID, IInteractor interactor)
        {
            InteractorContainerStub.RegisterInteractor(interactorID.ToString(), interactor);
        }

        public static void DeregisterInteractor(InteractorID interactorID)
        {
            InteractorContainerStub.DeregisterInteractor(interactorID.ToString());
        }
    }

    public class RayCastProviderSetup
    {
        public static IRaycastProvider RaycastProviderStub { get; private set; }

        public static void RayCastProviderStubSetup()
        {
            RaycastProviderStub = Substitute.For<IRaycastProvider>();
        }

        public static void StubRangedInteractionModuleForRaycastProviderStub(IRangedInteractionModule rangedInteractionModule)
        {
            RaycastProviderStub
                .Raycast(default, default, default, default)
                .ReturnsForAnyArgs(new RaycastResultWrapper(rangedInteractionModule, 0));
        }
    }

    public class PlayerInputContainerStubWrapper
    {
        public PlayerInputContainer PlayerInputContainer { get; private set; }

        public IPressableInput ChangeMode2D { get; private set; } = Substitute.For<IPressableInput>();

        // 2D player
        public IPressableInput InspectModeButton { get; private set; } = Substitute.For<IPressableInput>();
        public IPressableInput RangedClick2D { get; private set; } = Substitute.For<IPressableInput>();
        public IPressableInput Grab2D { get; private set; } = Substitute.For<IPressableInput>();
        public IPressableInput HandheldClick2D { get; private set; } = Substitute.For<IPressableInput>();
        public IScrollInput ScrollTickUp2D { get; private set; } = Substitute.For<IScrollInput>();
        public IScrollInput ScrollTickDown2D { get; private set; } = Substitute.For<IScrollInput>();

        // VR reset
        public IPressableInput ResetViewVR { get; private set; } = Substitute.For<IPressableInput>();

        // Left-hand VR
        public IValueInput<Vector3> HandVRLeftPosition { get; private set; } = Substitute.For<IValueInput<Vector3>>();
        public IValueInput<Quaternion> HandVRLeftRotation { get; private set; } = Substitute.For<IValueInput<Quaternion>>();
        public IPressableInput RangedClickVRLeft { get; private set; } = Substitute.For<IPressableInput>();
        public IPressableInput GrabVRLeft { get; private set; } = Substitute.For<IPressableInput>();
        public IPressableInput HandheldClickVRLeft { get; private set; } = Substitute.For<IPressableInput>();
        public IScrollInput ScrollTickUpVRLeft { get; private set; } = Substitute.For<IScrollInput>();
        public IScrollInput ScrollTickDownVRLeft { get; private set; } = Substitute.For<IScrollInput>();
        public IPressableInput HorizontalDragVRLeft { get; private set; } = Substitute.For<IPressableInput>();
        public IPressableInput VerticalDragVRLeft { get; private set; } = Substitute.For<IPressableInput>();

        // Right-hand VR
        public IValueInput<Vector3> HandVRRightPosition { get; private set; } = Substitute.For<IValueInput<Vector3>>();
        public IValueInput<Quaternion> HandVRRightRotation { get; private set; } = Substitute.For<IValueInput<Quaternion>>();
        public IPressableInput RangedClickVRRight { get; private set; } = Substitute.For<IPressableInput>();
        public IPressableInput GrabVRRight { get; private set; } = Substitute.For<IPressableInput>();
        public IPressableInput HandheldClickVRRight { get; private set; } = Substitute.For<IPressableInput>();
        public IScrollInput ScrollTickUpVRRight { get; private set; } = Substitute.For<IScrollInput>();
        public IScrollInput ScrollTickDownVRRight { get; private set; } = Substitute.For<IScrollInput>();
        public IPressableInput HorizontalDragVRRight { get; private set; } = Substitute.For<IPressableInput>();
        public IPressableInput VerticalDragVRRight { get; private set; } = Substitute.For<IPressableInput>();

        public PlayerInputContainerStubWrapper()
        {
            PlayerInputContainer = new PlayerInputContainer(
                changeMode2D: ChangeMode2D,
                inspectModeButton: InspectModeButton,
                rangedClick2D: RangedClick2D,
                grab2D: Grab2D,
                handheldClick2D: HandheldClick2D,
                scrollTickUp2D: ScrollTickUp2D,
                scrollTickDown2D: ScrollTickDown2D,
                resetViewVR: ResetViewVR,
                handVRLeftPosition: HandVRLeftPosition,
                handVRLeftRotation: HandVRLeftRotation,
                rangedClickVRLeft: RangedClickVRLeft,
                grabVRLeft: GrabVRLeft,
                handheldClickVRLeft: HandheldClickVRLeft,
                scrollTickUpVRLeft: ScrollTickUpVRLeft,
                scrollTickDownVRLeft: ScrollTickDownVRLeft,
                horizontalDragVRLeft: HorizontalDragVRLeft,
                verticalDragVRLeft: VerticalDragVRLeft,
                handVRRightPosition: HandVRRightPosition,
                handVRRightRotation: HandVRRightRotation,
                rangedClickVRRight: RangedClickVRRight,
                grabVRRight: GrabVRRight,
                handheldClickVRRight: HandheldClickVRRight,
                scrollTickUpVRRight: ScrollTickUpVRRight,
                scrollTickDownVRRight: ScrollTickDownVRRight,
                horizontalDragVRRight: HorizontalDragVRRight,
                verticalDragVRRight: VerticalDragVRRight
            );
        }
    }
}

