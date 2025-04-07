using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.Tests;
namespace VE2.Core.Tests
{
    [TestFixture]
    [Category("PlayerAndPressurePlateTests")]
    internal class PlayerAndPressurePlateTests : PlayerServiceSetupFixture
    {
        private IV_PressurePlate _pressurePlatePluginInterface => _v_pressurePlateProviderStub;
        private ICollideInteractionModuleProvider _pressurePlateCollideInterface => _v_pressurePlateProviderStub;
        private V_PressurePlateStub _v_pressurePlateProviderStub;
        private PluginActivatableScript _customerScript;

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            PressurePlateService pressurePlateService = new(
                new PressurePlateConfig(),
                new MultiInteractorActivatableState(),
                "debug");

            _v_pressurePlateProviderStub = new(pressurePlateService);

            _customerScript = Substitute.For<PluginActivatableScript>();
            _pressurePlatePluginInterface.OnActivate.AddListener(_customerScript.HandleActivateReceived);
            _pressurePlatePluginInterface.OnDeactivate.AddListener(_customerScript.HandleDeactivateReceived);
        }

        [Test]
        public void OnUserPressedDownAndReleased_OnCollidingWithPressurePlate_CustomerScriptTriggersOnActivateAndOnDeactivate([Random((ushort)0, ushort.MaxValue, 1)] ushort localClientID)
        {

        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {

        }
    }
}
