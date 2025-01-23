using UnityEngine;
using NSubstitute;
using NUnit.Framework;
using VE2.Core.VComponents.PluginInterfaces;
using VE2.Core.VComponents.NonInteractableInterfaces;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.Internal;
using VE2.Common.TransformWrapper;
using System.Collections.Generic;
using VE2.Common;
using VE2.Core.VComponents.InteractableFindables;

namespace VE2.Core.VComponents.Tests
{
    [TestFixture]
    [Category("Handheld Adjustable Tests")]
    public class LinearAdjustableTests
    {
        private IV_LinearAdjustable _linearAdjustablePluginInterface;
        private V_LinearAdjustableStub _v_linearAdjustableStub;

        private PluginAdjustableMock _customerScript;

        [OneTimeSetUp]
        public void SetUpOnce()
        {
            //Wire up the customer script to receive the events           
            _customerScript = Substitute.For<PluginAdjustableMock>();
        }

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            //create the handheld adjustable
            LinearAdjustableService linearAdjustable = LinearAdjustableServiceFactory.Create();
            _v_linearAdjustableStub = new(linearAdjustable);

            //hook up interfaces
            _linearAdjustablePluginInterface = _v_linearAdjustableStub;

            //wire up the customer script to receive the events
            _linearAdjustablePluginInterface.OnValueAdjusted.AddListener(_customerScript.HandleValueAdjusted);
        }

        [Test]
        public void LinearAdjustable_WhenAdjustedByPlugin_EmitsToPlugin([Random(0f, 1f, 1)] float randomValue)
        {
            //set the adjustable value, Check customer received the value adjusted, and that the interactorID is set
            _linearAdjustablePluginInterface.SpatialValue = randomValue;
            _customerScript.Received(1).HandleValueAdjusted(randomValue);
            Assert.IsTrue(_linearAdjustablePluginInterface.OutputValue == randomValue);
            Assert.AreEqual(_linearAdjustablePluginInterface.MostRecentInteractingClientID, ushort.MaxValue);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();
            _linearAdjustablePluginInterface.OnValueAdjusted.RemoveAllListeners();

            _v_linearAdjustableStub.TearDown();
            _linearAdjustablePluginInterface = null;
        }

        [OneTimeTearDown]
        public void TearDownOnce() { }
    }

    public class V_LinearAdjustableStub : IV_LinearAdjustable, IRangedGrabPlayerInteractableIntegrator
    {
        #region Plugin Interfaces
        IAdjustableStateModule IV_LinearAdjustable._AdjustableStateModule => _linearAdjustable.AdjustableStateModule;
        IFreeGrabbableStateModule IV_LinearAdjustable._FreeGrabbableStateModule => _linearAdjustable.FreeGrabbableStateModule;
        IRangedAdjustableInteractionModule IV_LinearAdjustable._RangedAdjustableModule => _linearAdjustable.RangedAdjustableInteractionModule;
        #endregion

        #region Player Interfaces
        IRangedInteractionModule IRangedPlayerInteractableIntegrator.RangedInteractionModule => _linearAdjustable.RangedAdjustableInteractionModule;
        #endregion

        public float MinimumSpatialValue { get => _linearAdjustable.MinimumSpatialValue; set => _linearAdjustable.MinimumSpatialValue = value; }
        public float MaximumSpatialValue { get => _linearAdjustable.MaximumSpatialValue; set => _linearAdjustable.MaximumSpatialValue = value; }
        public float SpatialValue { get => _linearAdjustable.SpatialValue; set => _linearAdjustable.SpatialValue = value; }
        public int NumberOfValues { get => _linearAdjustable.NumberOfValues; set => _linearAdjustable.NumberOfValues = value; }

        protected LinearAdjustableService _linearAdjustable = null;

        public V_LinearAdjustableStub(LinearAdjustableService linearAdjustable)
        {
            _linearAdjustable = linearAdjustable;
        }

        public void TearDown()
        {
            _linearAdjustable.TearDown();
            _linearAdjustable = null;
        }
    }

    public class LinearAdjustableServiceFactory
    {
        public static LinearAdjustableService Create(
            ITransformWrapper transformWrapper = null,
            List<IHandheldInteractionModule> handheldInteractions = null,
            LinearAdjustableConfig config = null,
            AdjustableState adjustableState = null,
            FreeGrabbableState grabbableState = null,
            string id = "debug",
            WorldStateModulesContainer worldStateModulesContainer = null,
            InteractorContainer interactorContainer = null
        )
        {
            transformWrapper ??= Substitute.For<ITransformWrapper>();
            handheldInteractions ??= new List<IHandheldInteractionModule>();
            config ??= new LinearAdjustableConfig();
            adjustableState ??= new AdjustableState();
            grabbableState ??= new FreeGrabbableState();
            worldStateModulesContainer ??= new WorldStateModulesContainer();
            interactorContainer ??= new InteractorContainer();

            LinearAdjustableService linearAdjustable = new(transformWrapper, handheldInteractions, config, adjustableState, grabbableState, id, worldStateModulesContainer, interactorContainer);

            return linearAdjustable;
        }
    }
}
