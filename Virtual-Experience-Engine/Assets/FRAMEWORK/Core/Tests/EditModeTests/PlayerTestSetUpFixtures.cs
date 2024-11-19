using NUnit.Framework;
using NSubstitute;
using VE2.Common;
using static VE2.Common.CommonSerializables;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.Player;
using UnityEngine;

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
        public static IInputHandler InputHandlerStub { get; private set; }

        [OneTimeSetUp]
        public void InputHandlerStubSetupOnce()
        {
            //Stub out the input handler    
            InputHandlerStub = Substitute.For<IInputHandler>();
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
                .TryGetRangedInteractionModule(default, default, out Arg.Any<RaycastResultWrapper>(), default, default)
                .ReturnsForAnyArgs(x =>
                {
                    x[2] = new RaycastResultWrapper(rangedInteractionModule, 0);
                    return true;
                });
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
                PlayerSettingsProviderSetup.PlayerSettingsProviderStub,
                Substitute.For<IPlayerAppearanceOverridesProvider>(),
                MultiplayerSupportSetup.MultiplayerSupportStub,
                InputHandlerSetup.InputHandlerStub,
                RayCastProviderSetup.RaycastProviderStub, 
                Substitute.For<IXRManagerWrapper>()
            );
        }
    }
}

