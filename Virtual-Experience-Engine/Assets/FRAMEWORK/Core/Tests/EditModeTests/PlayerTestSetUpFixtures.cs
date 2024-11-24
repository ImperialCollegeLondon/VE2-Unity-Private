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
    //public class PlayerSetup { }

    [SetUpFixture]
    public class PlayerSettingsProviderSetup
    {
        public static IPlayerSettingsProvider PlayerSettingsProviderStub { get; private set; }

        [OneTimeSetUp]
        public void PlayerSettingsProviderStubSetupOnce()
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

    [SetUpFixture]
    public class InputHandlerSetup
    {
        public static PlayerInputContainerStubWrapper PlayerInputContainerStubWrapper { get; private set; }

        [OneTimeSetUp]
        public void InputHandlerStubSetupOnce()
        {
            //Stub out the input handler    
            PlayerInputContainerStubWrapper = new();
        }
    }
    
    [SetUpFixture]
    public class MultiplayerSupportSetup
    {
        public static IMultiplayerSupport MultiplayerSupportStub { get; private set; }
        public static InteractorID InteractorID { get; private set; }
        public static string InteractorGameobjectName { get; private set; }

        [OneTimeSetUp]
        public void MultiplayerSupportStubSetupOnce()
        {
            //Stub out the multiplayer support
            System.Random random = new();
            ushort localClientID = (ushort)random.Next(0, ushort.MaxValue);

            MultiplayerSupportStub = Substitute.For<IMultiplayerSupport>();
            MultiplayerSupportStub.IsConnectedToServer.Returns(true);
            InteractorID = new(localClientID, InteractorType.Mouse2D);
            InteractorGameobjectName = $"Interactor{InteractorID.ClientID}-{InteractorID.InteractorType}";
        }

        public static void StubLocalClientIDForMultiplayerSupportStub(ushort localClientID)
        {
            MultiplayerSupportStub.LocalClientID.Returns(localClientID);
        }
    }

    [SetUpFixture]
    public class InteractorSetup
    {
        public static IInteractor InteractorStub { get; private set; }
        public static GameObject InteractorGameObject { get; private set; }

        [OneTimeSetUp]
        public void InteractorStubSetupOnce()
        {
            InteractorStub = Substitute.For<IInteractor>();
            InteractorGameObject = new();
        }
    }

    [SetUpFixture]
    public class GameObjectFindProviderSetup
    {
        public static IGameObjectFindProvider GameObjectFindProviderStub { get; private set; }

        [OneTimeSetUp]
        public void GameObjectFindProviderStubSetupOnce()
        {
            //Stub out the game object find provider
            GameObjectFindProviderStub = Substitute.For<IGameObjectFindProvider>();
        }

        public static void StubFindGameObjectForGameObjectFindProviderStub(GameObject interactorGameObject)
        {
            GameObjectFindProviderStub.FindGameObject(MultiplayerSupportSetup.InteractorGameobjectName).Returns(interactorGameObject);
            GameObjectFindProviderStub.TryGetComponent<IInteractor>(interactorGameObject, out Arg.Any<IInteractor>()).Returns(x =>
            {
                x[1] = InteractorSetup.InteractorStub;
                return true;
            });
        }
    }

    [SetUpFixture]
    public class RayCastProviderSetup
    {
        public static IRaycastProvider RaycastProviderStub { get; private set; }

        [OneTimeSetUp]
        public void RayCastProviderStubSetupOnce()
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

    [SetUpFixture]
    public class PlayerServiceSetup
    {
        public static PlayerService PlayerServiceStub { get; private set; }

        [OneTimeSetUp]
        public void SetUpPlayerServiceStub()
        {
            //Create the player (2d)
            PlayerServiceStub = new(
                new PlayerTransformData(),
                new PlayerStateConfig(),
                false,
                true,
                new PlayerStateModuleContainer(),
                new InteractorContainer(),
                PlayerSettingsProviderSetup.PlayerSettingsProviderStub,
                Substitute.For<IPlayerAppearanceOverridesProvider>(),
                MultiplayerSupportSetup.MultiplayerSupportStub,
                InputHandlerSetup.PlayerInputContainerStubWrapper.PlayerInputContainer,
                RayCastProviderSetup.RaycastProviderStub, 
                Substitute.For<IXRManagerWrapper>()
            );
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

