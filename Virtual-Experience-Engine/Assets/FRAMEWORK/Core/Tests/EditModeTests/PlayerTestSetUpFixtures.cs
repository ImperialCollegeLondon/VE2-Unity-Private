using NUnit.Framework;
using NSubstitute;
using UnityEngine;
using VE2.Core.VComponents.API;
using VE2.Core.Player.Internal;
using VE2.Core.Player.API;
using static VE2.Core.Player.API.PlayerSerializables;

namespace VE2.Core.Tests
{
    [SetUpFixture]
    internal class LocalClientIDProviderSetup
    {
        public static ILocalClientIDProvider LocalClientIDProviderStub { get; private set; }
        public static InteractorID InteractorID { get; private set; }
        public static string InteractorGameobjectName { get; private set; }

        [OneTimeSetUp]
        public static void MultiplayerSupportStubSetupOnce()
        {
            //Stub out the multiplayer support
            System.Random random = new();
            ushort localClientID = (ushort)random.Next(0, ushort.MaxValue);

            LocalClientIDProviderStub = Substitute.For<ILocalClientIDProvider>();
            LocalClientIDProviderStub.IsClientIDReady.Returns(true);
            LocalClientIDProviderStub.LocalClientID.Returns(localClientID);
            InteractorID = new(localClientID, InteractorType.Mouse2D);
            InteractorGameobjectName = $"Interactor{InteractorID.ClientID}-{InteractorID.InteractorType}";
        }

        public static void StubLocalClientIDForMultiplayerSupportStub(ushort localClientID)
        {
            LocalClientIDProviderStub.LocalClientID.Returns(localClientID);
        }
    }

    [SetUpFixture]
    internal class InteractorSetup
    {
        public static IInteractor InteractorStub { get; private set; }
        public static GameObject InteractorGameObject { get; private set; }

        [OneTimeSetUp]
        public static void InteractorStubSetupOnce()
        {
            InteractorStub = Substitute.For<IInteractor>();
            InteractorGameObject = new();
        }
    }

    [SetUpFixture]
    internal class InteractorContainerSetup
    {
        public static InteractorContainer InteractorContainer { get; private set; }

        [OneTimeSetUp]
        public static void InteractorContainerSetupOnce()
        {
            InteractorContainer = new();
            InteractorContainer.RegisterInteractor(LocalClientIDProviderSetup.InteractorID.ToString(), InteractorSetup.InteractorStub);
        }
    }

    [SetUpFixture]
    internal class RayCastProviderSetup
    {
        public static IRaycastProvider RaycastProviderStub { get; private set; }

        [OneTimeSetUp]
        public static void RayCastProviderStubSetupOnce()
        {
            RaycastProviderStub = Substitute.For<IRaycastProvider>();
        }

        public static void StubRangedInteractionModuleForRaycastProviderStub(IRangedInteractionModule rangedInteractionModule)
        {
            RaycastProviderStub
                .Raycast(default, default, default, default)
                .ReturnsForAnyArgs(new RaycastResultWrapper(rangedInteractionModule, null, 0));
        }
    }

    [SetUpFixture]
    internal class PlayerPersistentDataHandlerSetup
    {
        public static IPlayerPersistentDataHandler PlayerPersistentDataHandlerStub { get; private set; }

        [OneTimeSetUp]
        public void PlayerPersistentDataHandlerStubSetupOnce()
        {
            PlayerPersistentDataHandlerStub = Substitute.For<IPlayerPersistentDataHandler>();
            PlayerPersistentDataHandlerStub.PlayerPresentationConfig.Returns(new PlayerPresentationConfig());
        }
    }

    [SetUpFixture]
    public class PlayerInputContainerSetup
    {
        public static PlayerInputContainer PlayerInputContainerStub { get; private set; }

        public static IPressableInput ChangeMode2D { get; private set; } = Substitute.For<IPressableInput>();

        // 2D player
        public static IPressableInput InspectModeButton { get; private set; } = Substitute.For<IPressableInput>();
        public static IPressableInput RangedClick2D { get; private set; } = Substitute.For<IPressableInput>();
        public static IPressableInput Grab2D { get; private set; } = Substitute.For<IPressableInput>();
        public static IPressableInput HandheldClick2D { get; private set; } = Substitute.For<IPressableInput>();
        public static IScrollInput ScrollTickUp2D { get; private set; } = Substitute.For<IScrollInput>();
        public static IScrollInput ScrollTickDown2D { get; private set; } = Substitute.For<IScrollInput>();

        // VR reset
        public static IPressableInput ResetViewVR { get; private set; } = Substitute.For<IPressableInput>();

        // Left-hand VR
        public static IValueInput<Vector3> HandVRLeftPosition { get; private set; } = Substitute.For<IValueInput<Vector3>>();
        public static IValueInput<Quaternion> HandVRLeftRotation { get; private set; } = Substitute.For<IValueInput<Quaternion>>();
        public static IPressableInput RangedClickVRLeft { get; private set; } = Substitute.For<IPressableInput>();
        public static IPressableInput GrabVRLeft { get; private set; } = Substitute.For<IPressableInput>();
        public static IPressableInput HandheldClickVRLeft { get; private set; } = Substitute.For<IPressableInput>();
        public static IScrollInput ScrollTickUpVRLeft { get; private set; } = Substitute.For<IScrollInput>();
        public static IScrollInput ScrollTickDownVRLeft { get; private set; } = Substitute.For<IScrollInput>();
        public static IPressableInput HorizontalDragVRLeft { get; private set; } = Substitute.For<IPressableInput>();
        public static IPressableInput VerticalDragVRLeft { get; private set; } = Substitute.For<IPressableInput>();
        public static IStickPressInput StickPressHorizontalLeftDirectionVRLeft { get; private set; } = Substitute.For<IStickPressInput>();
        public static IStickPressInput StickPressHorizontalRightDirectionVRLeft { get; private set; } = Substitute.For<IStickPressInput>();
        public static IPressableInput StickPressVerticalVRLeft { get; private set; } = Substitute.For<IPressableInput>();
        public static IValueInput<Vector2> TeleportDirectionVRLeft { get; private set; } = Substitute.For<IValueInput<Vector2>>();

        // Right-hand VR
        public static IValueInput<Vector3> HandVRRightPosition { get; private set; } = Substitute.For<IValueInput<Vector3>>();
        public static IValueInput<Quaternion> HandVRRightRotation { get; private set; } = Substitute.For<IValueInput<Quaternion>>();
        public static IPressableInput RangedClickVRRight { get; private set; } = Substitute.For<IPressableInput>();
        public static IPressableInput GrabVRRight { get; private set; } = Substitute.For<IPressableInput>();
        public static IPressableInput HandheldClickVRRight { get; private set; } = Substitute.For<IPressableInput>();
        public static IScrollInput ScrollTickUpVRRight { get; private set; } = Substitute.For<IScrollInput>();
        public static IScrollInput ScrollTickDownVRRight { get; private set; } = Substitute.For<IScrollInput>();
        public static IPressableInput HorizontalDragVRRight { get; private set; } = Substitute.For<IPressableInput>();
        public static IPressableInput VerticalDragVRRight { get; private set; } = Substitute.For<IPressableInput>(); 
        public static IStickPressInput StickPressHorizontalLeftDirectionVRRight { get; private set; } = Substitute.For<IStickPressInput>();
        public static IStickPressInput StickPressHorizontalRightDirectionVRRight { get; private set; } = Substitute.For<IStickPressInput>();
        public static IPressableInput StickPressVerticalVRRight { get; private set; } = Substitute.For<IPressableInput>();
        public static IValueInput<Vector2> TeleportDirectionVRRight { get; private set; } = Substitute.For<IValueInput<Vector2>>();

        [OneTimeSetUp]
        public static void SetupPlayerInputContainerStubWrapper()
        {
            PlayerInputContainerStub = new PlayerInputContainer(
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
                verticalDragVRRight: VerticalDragVRRight,
                stickPressHorizontalLeftDirectionVRLeft: StickPressHorizontalLeftDirectionVRLeft,
                stickPressHorizontalRightDirectionVRLeft: StickPressHorizontalRightDirectionVRLeft,
                stickPressHorizontalLeftDirectionVRRight: StickPressHorizontalLeftDirectionVRRight,
                stickPressHorizontalRightDirectionVRRight: StickPressHorizontalRightDirectionVRRight,
                stickPressVerticalVRLeft: StickPressVerticalVRLeft,
                teleportDirectionVRLeft: TeleportDirectionVRLeft,
                stickPressVerticalVRRight: StickPressVerticalVRRight,
                teleportDirectionVRRight: TeleportDirectionVRRight
            );
        }
    }

    //We want to repeat this setup for every test
    //Otherwise, we may find that the player's state carries over between tests!
    internal abstract class PlayerServiceSetupFixture
    {
        private PlayerService _playerService;
        public IPlayerService PlayerService => _playerService;

        [SetUp]
        public void SetUpPlayerServiceBeforeEachTest()
        {
            _playerService = new PlayerService(
                new PlayerTransformData(),
                new PlayerConfig(),
                InteractorContainerSetup.InteractorContainer,
                PlayerPersistentDataHandlerSetup.PlayerPersistentDataHandlerStub,
                LocalClientIDProviderSetup.LocalClientIDProviderStub,
                PlayerInputContainerSetup.PlayerInputContainerStub,
                RayCastProviderSetup.RaycastProviderStub, 
                Substitute.For<IXRManagerWrapper>()
            );
        }

        [TearDown]
        public void TearDownPlayerServiceAfterEachTest()
        {
            _playerService.TearDown();
            _playerService = null;
        }
    }
}

