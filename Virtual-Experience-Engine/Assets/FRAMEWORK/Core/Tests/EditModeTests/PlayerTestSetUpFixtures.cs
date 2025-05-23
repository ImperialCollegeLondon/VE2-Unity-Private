using NUnit.Framework;
using NSubstitute;
using UnityEngine;
using VE2.Core.VComponents.API;
using VE2.Core.Player.Internal;
using VE2.Core.Player.API;
using static VE2.Core.Player.API.PlayerSerializables;
using VE2.Core.UI.API;
using System.Collections.Generic;
using VE2.Common.API;
using VE2.Common.Shared;

namespace VE2.Core.Tests
{
    internal class LocalClientIDWrapperSetup
    {
        public static ILocalClientIDWrapper LocalClientIDWrapper { get; private set; }
        public static InteractorID InteractorID { get; private set; }
        public static ushort LocalClientID => LocalClientIDWrapper.Value;
        public static string InteractorGameobjectName { get; private set; }

        public static void LocalClientIDWrapperStubSetupOnce()
        {
            //Stub out the multiplayer support
            System.Random random = new();
            ushort localClientID = (ushort)random.Next(0, ushort.MaxValue);

            LocalClientIDWrapper = Substitute.For<ILocalClientIDWrapper>();
            LocalClientIDWrapper.IsClientIDReady.Returns(true);
            LocalClientIDWrapper.Value.Returns(localClientID);
            InteractorID = new(localClientID, InteractorType.Mouse2D);
            InteractorGameobjectName = $"Interactor{InteractorID.ClientID}-{InteractorID.InteractorType}";
        }

        public static void StubLocalClientIDForMultiplayerSupportStub(ushort localClientID)
        {
            LocalClientIDWrapper.Value.Returns(localClientID);
        }
    }

    internal class InteractorSetup
    {
        public static IInteractor InteractorStub { get; private set; }
        public static GameObject InteractorGameObject { get; private set; }

        public static void InteractorStubSetupOnce()
        {
            InteractorStub = Substitute.For<IInteractor>();
            InteractorGameObject = new();
        }
    }

    internal class InteractorContainerSetup
    {
        public static HandInteractorContainer InteractorContainer { get; private set; }

        public static void InteractorContainerSetupOnce()
        {
            InteractorContainer = new();
            //InteractorContainer.RegisterInteractor(LocalClientIDWrapperSetup.InteractorID.ToString(), InteractorSetup.InteractorStub);
        }
    }

    internal class LocalPlayerSyncableContainerSetup
    {
        public static ILocalPlayerSyncableContainer LocalPlayerSyncableContainerStub { get; private set; }

        public static void LocalPlayerSyncableContainerStubSetupOnce()
        {
            LocalPlayerSyncableContainerStub = new LocalPlayerSyncableContainer();
            //LocalPlayerSyncableContainerStub.LocalPlayerID.Returns(LocalClientIDWrapperSetup.InteractorID.ClientID);
        }
    }

    internal class GrabInteractableContainerSetup
    {
        public static IGrabInteractablesContainer GrabInteractableContainer { get; private set; }

        public static void GrabInteractableContainerStubSetupOnce()
        {
            GrabInteractableContainer = new GrabInteractablesContainer();
        }
    }

    internal class RayCastProviderSetup
    {
        public static IRaycastProvider RaycastProviderStub { get; private set; }

        public static void RayCastProviderStubSetupOnce()
        {
            RaycastProviderStub = Substitute.For<IRaycastProvider>();
        }

        public static void StubRangedInteractionModuleForRaycast(IRangedInteractionModule rangedInteractionModule)
        {
            RaycastProviderStub
                .Raycast(default, default, default, default)
                .ReturnsForAnyArgs(new RaycastResultWrapper(rangedInteractionModule, null, 0, true));
        }

        public static void StubRangedInteractionModuleForSpherecastAll(IRangedInteractionModule rangedInteractionModule)
        {
            RaycastProviderStub
                .SphereCastAll(default, default, default, default, default)
                .ReturnsForAnyArgs(new RaycastResultWrapper(rangedInteractionModule, null, 0, true));
        }
    }

    internal class CollisionDetectorFactoryStubSetup
    {
        public static CollisionDetectorFactoryStub CollisionDetectorFactoryStub { get; private set; }

        public static void CollisionDetectorStubSetupOnce()
        {
            CollisionDetectorFactoryStub = new CollisionDetectorFactoryStub();
        }
    }

    internal class CollisionDetectorFactoryStub : ICollisionDetectorFactory
    {
        internal Dictionary<ColliderType, ICollisionDetector> CollisionDetectorStubs { get; } = new();

        ICollisionDetector ICollisionDetectorFactory.CreateCollisionDetector(Collider collider, ColliderType colliderType, LayerMask collisionLayers)
        {
            ICollisionDetector collisionDetector = Substitute.For<ICollisionDetector>();
            collisionDetector.ColliderType.Returns(colliderType);
            CollisionDetectorStubs.Add(colliderType, collisionDetector);

            return collisionDetector;
        }
    }

    internal class PlayerPersistentDataHandlerSetup
    {
        public static IPlayerPersistentDataHandler PlayerPersistentDataHandlerStub { get; private set; }

        public static void PlayerPersistentDataHandlerStubSetupOnce()
        {
            PlayerPersistentDataHandlerStub = Substitute.For<IPlayerPersistentDataHandler>();
            PlayerPersistentDataHandlerStub.PlayerPresentationConfig.Returns(new PlayerPresentationConfig());
        }
    }

    internal class PlayerInputContainerSetup
    {
        public static PlayerInputContainer PlayerInputContainerStub { get; private set; }

        public static IPressableInput ChangeMode2D { get; private set; } = Substitute.For<IPressableInput>();

        // 2D player
        public static IPressableInput RangedClick2D { get; private set; } = Substitute.For<IPressableInput>();
        public static IPressableInput Grab2D { get; private set; } = Substitute.For<IPressableInput>();
        public static IPressableInput HandheldClick2D { get; private set; } = Substitute.For<IPressableInput>();
        public static IPressableInput InspectModeInput { get; private set; } = Substitute.For<IPressableInput>();
        public static IScrollInput ScrollTickUp2D { get; private set; } = Substitute.For<IScrollInput>();
        public static IScrollInput ScrollTickDown2D { get; private set; } = Substitute.For<IScrollInput>();
        public static IValueInput<Vector2> MousePosition { get; private set; } = Substitute.For<IValueInput<Vector2>>();

        // VR reset
        public static IDelayedChargableInput ResetViewVR { get; private set; } = Substitute.For<IDelayedChargableInput>();

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

        public static void SetupPlayerInputContainerStubWrapper()
        {
            PlayerInputContainerStub = new PlayerInputContainer(
                changeMode2D: ChangeMode2D,
                rangedClick2D: RangedClick2D,
                grab2D: Grab2D,
                handheldClick2D: HandheldClick2D,
                inspectModeInput: InspectModeInput,
                scrollTickUp2D: ScrollTickUp2D,
                scrollTickDown2D: ScrollTickDown2D,
                mouseDeltaInput: MousePosition,
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
    [SetUpFixture]
    internal abstract class PlayerServiceSetupFixture
    {
        private PlayerService _playerService;
        public IPlayerService PlayerService => _playerService;

        [OneTimeSetUp] //This is done to remove the crazy Hierarchy of tests in the Unity Test Runner
        public void SetUpPlayerServiceOnce()
        {
            LocalClientIDWrapperSetup.LocalClientIDWrapperStubSetupOnce();
            InteractorSetup.InteractorStubSetupOnce();
            InteractorContainerSetup.InteractorContainerSetupOnce();
            RayCastProviderSetup.RayCastProviderStubSetupOnce();
            CollisionDetectorFactoryStubSetup.CollisionDetectorStubSetupOnce();
            PlayerPersistentDataHandlerSetup.PlayerPersistentDataHandlerStubSetupOnce();
            PlayerInputContainerSetup.SetupPlayerInputContainerStubWrapper();
            LocalPlayerSyncableContainerSetup.LocalPlayerSyncableContainerStubSetupOnce();
            GrabInteractableContainerSetup.GrabInteractableContainerStubSetupOnce();
        }

        [SetUp]
        public void SetUpPlayerServiceBeforeEachTest()
        {
            _playerService = new PlayerService(
                new PlayerTransformData(),
                new PlayerConfig(),
                InteractorContainerSetup.InteractorContainer,
                PlayerPersistentDataHandlerSetup.PlayerPersistentDataHandlerStub,
                LocalClientIDWrapperSetup.LocalClientIDWrapper,
                LocalPlayerSyncableContainerSetup.LocalPlayerSyncableContainerStub,
                GrabInteractableContainerSetup.GrabInteractableContainer,
                PlayerInputContainerSetup.PlayerInputContainerStub,
                RayCastProviderSetup.RaycastProviderStub, 
                CollisionDetectorFactoryStubSetup.CollisionDetectorFactoryStub,
                Substitute.For<IXRManagerWrapper>(),
                Substitute.For<IPrimaryUIServiceInternal>(),
                Substitute.For<ISecondaryUIServiceInternal>()
            );
        }

        [TearDown]
        public void TearDownPlayerServiceAfterEachTest()
        {
            CollisionDetectorFactoryStubSetup.CollisionDetectorFactoryStub.CollisionDetectorStubs.Clear();

            _playerService.TearDown();
            _playerService = null;
        }
    }
}

