using UnityEngine;
using NSubstitute;
using NUnit.Framework;
using VE2.Core.VComponents.Internal;
using VE2.Common.TransformWrapper;
using System.Collections.Generic;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Tests
{
    [TestFixture]
    [Category("Rotational Adjustable Tests")]
    public class RotationalAdjustableTests
    {
        private IV_RotationalAdjustable _rotationAdjustmentPluginInterface => _v_rotationalAdjustableProviderStub;
        private V_RotationalAdjustableProviderStub _v_rotationalAdjustableProviderStub;

        private PluginAdjustableScript _customerScript;

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            //create the handheld adjustable
            RotationalAdjustableService rotationalAdjustable = new(
                Substitute.For<ITransformWrapper>(),
                new List<IHandheldInteractionModule>(),
                new RotationalAdjustableConfig(),
                new AdjustableState(),
                new GrabbableState(),
                "debug",
                Substitute.For<IWorldStateSyncService>(),
                new HandInteractorContainer());
            _v_rotationalAdjustableProviderStub = new(rotationalAdjustable);

            //wire up the customer script to receive the events       
            _customerScript = Substitute.For<PluginAdjustableScript>();
            _rotationAdjustmentPluginInterface.OnValueAdjusted.AddListener(_customerScript.HandleValueAdjusted);
        }

        [Test]
        public void LinearAdjustable_WhenAdjustedByPlugin_EmitsToPlugin([Random(0f, 1f, 1)] float randomValue)
        {
            //set the adjustable value, Check customer received the value adjusted, and that the interactorID is set
            _rotationAdjustmentPluginInterface.SpatialValue = randomValue;
            _customerScript.Received(1).HandleValueAdjusted(randomValue);
            Assert.IsTrue(_rotationAdjustmentPluginInterface.Value == randomValue);
            Assert.AreEqual(_rotationAdjustmentPluginInterface.MostRecentInteractingClientID, ushort.MaxValue);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();
            _rotationAdjustmentPluginInterface.OnValueAdjusted.RemoveAllListeners();

            _v_rotationalAdjustableProviderStub.TearDown();
        }

        [OneTimeTearDown]
        public void TearDownOnce() { }
    }

    internal class V_RotationalAdjustableProviderStub : IV_RotationalAdjustable, IRangedGrabInteractionModuleProvider
    {
        #region Plugin Interfaces
        IAdjustableStateModule IV_RotationalAdjustable._AdjustableStateModule => _rotationalAdjustable.AdjustableStateModule;
        IGrabbableStateModule IV_RotationalAdjustable._GrabbableStateModule => _rotationalAdjustable.GrabbableStateModule;
        IRangedAdjustableInteractionModule IV_RotationalAdjustable._RangedAdjustableModule => _rotationalAdjustable.RangedAdjustableInteractionModule;
        #endregion

        #region Player Interfaces
        IRangedInteractionModule IRangedInteractionModuleProvider.RangedInteractionModule => _rotationalAdjustable.RangedAdjustableInteractionModule;
        #endregion

        public float MinimumSpatialValue { get => _rotationalAdjustable.MinimumSpatialValue; set => _rotationalAdjustable.MinimumSpatialValue = value; }
        public float MaximumSpatialValue { get => _rotationalAdjustable.MaximumSpatialValue; set => _rotationalAdjustable.MaximumSpatialValue = value; }
        public float SpatialValue { get => _rotationalAdjustable.SpatialValue; set => _rotationalAdjustable.SpatialValue = value; }
        public int NumberOfValues { get => _rotationalAdjustable.NumberOfValues; set => _rotationalAdjustable.NumberOfValues = value; }

        protected RotationalAdjustableService _rotationalAdjustable = null;

        public V_RotationalAdjustableProviderStub(RotationalAdjustableService rotationalAdjustable)
        {
            _rotationalAdjustable = rotationalAdjustable;
        }

        public void TearDown()
        {
            _rotationalAdjustable.TearDown();
            _rotationalAdjustable = null;
        }
    }
}
