using UnityEngine;
using NSubstitute;
using NUnit.Framework;
using VE2.Core.VComponents.Internal;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using VE2.Common.Shared;
using VE2.Common.API;

namespace VE2.Core.VComponents.Tests
{
    [TestFixture]
    [Category("Linear Adjustable Tests")]
    internal class LinearAdjustableTests
    {
        private IV_LinearAdjustable _linearAdjustablePluginInterface => _v_linearAdjustableProviderStub;
        private V_LinearAdjustableProviderStub _v_linearAdjustableProviderStub;

        private PluginAdjustableScript _customerScript;

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            //create the handheld adjustable
            LinearAdjustableService linearAdjustable = new(
                Substitute.For<ITransformWrapper>(),
                new List<IHandheldInteractionModule>(),
                new LinearAdjustableConfig(),
                new AdjustableState(),
                new GrabbableState(),
                "debug",
                Substitute.For<IWorldStateSyncableContainer>(),
                Substitute.For<IGrabInteractablesContainer>(),
                new HandInteractorContainer(),
                Substitute.For<IClientIDWrapper>());

            _v_linearAdjustableProviderStub = new(linearAdjustable);

            //wire up the customer script to receive the events     
            _customerScript = Substitute.For<PluginAdjustableScript>();
            _linearAdjustablePluginInterface.OnValueAdjusted.AddListener(_customerScript.HandleValueAdjusted);
        }

        [Test]
        public void LinearAdjustable_WhenAdjustedByPlugin_EmitsToPlugin([Random(0f, 1f, 1)] float randomValue)
        {
            //set the adjustable value, Check customer received the value adjusted, and that the interactorID reflects programmatic activation (ie, null!)
            _linearAdjustablePluginInterface.SpatialValue = randomValue;
            _customerScript.Received(1).HandleValueAdjusted(randomValue);
            Assert.IsTrue(_linearAdjustablePluginInterface.Value == randomValue);
            Assert.AreEqual(_linearAdjustablePluginInterface.MostRecentInteractingClientID, null);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();
            _linearAdjustablePluginInterface.OnValueAdjusted.RemoveAllListeners();

            _v_linearAdjustableProviderStub.TearDown();
        }

        [OneTimeTearDown]
        public void TearDownOnce() { }
    }

    internal class V_LinearAdjustableProviderStub : IV_LinearAdjustable, IRangedGrabInteractionModuleProvider
    {
        #region Plugin Interfaces
        IAdjustableStateModule IV_LinearAdjustable._AdjustableStateModule => _linearAdjustable.AdjustableStateModule;
        IGrabbableStateModule IV_LinearAdjustable._GrabbableStateModule => _linearAdjustable.FreeGrabbableStateModule;
        IRangedAdjustableInteractionModule IV_LinearAdjustable._RangedAdjustableModule => _linearAdjustable.RangedAdjustableInteractionModule;
        #endregion

        #region Player Interfaces
        IRangedInteractionModule IRangedInteractionModuleProvider.RangedInteractionModule => _linearAdjustable.RangedAdjustableInteractionModule;
        #endregion

        public float MinimumSpatialValue { get => _linearAdjustable.MinimumSpatialValue; set => _linearAdjustable.MinimumSpatialValue = value; }
        public float MaximumSpatialValue { get => _linearAdjustable.MaximumSpatialValue; set => _linearAdjustable.MaximumSpatialValue = value; }
        public float SpatialValue { get => _linearAdjustable.SpatialValue; set => _linearAdjustable.SpatialValue = value; }
        public int NumberOfValues { get => _linearAdjustable.NumberOfValues; set => _linearAdjustable.NumberOfValues = value; }

        protected LinearAdjustableService _linearAdjustable = null;

        public V_LinearAdjustableProviderStub(LinearAdjustableService linearAdjustable)
        {
            _linearAdjustable = linearAdjustable;
        }

        public void TearDown()
        {
            _linearAdjustable.TearDown();
            _linearAdjustable = null;
        }
    }
}
