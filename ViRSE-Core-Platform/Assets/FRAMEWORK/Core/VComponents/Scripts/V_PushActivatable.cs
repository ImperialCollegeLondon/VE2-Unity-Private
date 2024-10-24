using System;
using System.Linq;
using UnityEngine;
using ViRSE.Core.Shared;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.Core.VComponents
{
    [Serializable]
    public class PushActivatableConfig
    {
        [SpaceArea(spaceAfter: 15), SerializeField, IgnoreParent] public ActivatableStateConfig StateConfig = new();
        [SpaceArea(spaceAfter: 15), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
        [SerializeField, IgnoreParent] public RangedInteractionConfig RangedInteractionConfig = new();
    }

    public abstract class BaseStateHolder : MonoBehaviour //TODO - do we really need this?
    {
        public abstract BaseStateConfig BaseStateConfig { get; }
    }

    public class V_PushActivatable : BaseStateHolder, IPushActivatable, IRangedClickPlayerInteractableImplementor, ICollidePlayerInteratableImplementor
    {
        [SerializeField, HideLabel, IgnoreParent] private PushActivatableConfig _config = new(); 
        [SerializeField, HideInInspector] private SingleInteractorActivatableState _state = new();

        private PushActivatable _pushActivatable = null;

        public override BaseStateConfig BaseStateConfig => _config.StateConfig;

        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule ISingleInteractorActivatableStateModuleImplementor._module => _pushActivatable.StateModule;
        IGeneralInteractionModule IGeneralInteractionModuleImplementor._module => _pushActivatable.GeneralInteractionModule;
        IRangedInteractionModule IRangedInteractionModuleImplementor._module => _pushActivatable.RangedClickInteractionModule;
        #endregion

        #region Player Rig Interfaces
        IGeneralPlayerInteractable IGeneralPlayerInteractableImplementor.GeneralPlayerInteractable => _pushActivatable.GeneralInteractionModule;
        IRangedPlayerInteractable IRangedPlayerInteractableImplementor.RangedPlayerInteractable => _pushActivatable.RangedClickInteractionModule;
        IRangedClickPlayerInteractable IRangedClickPlayerInteractableImplementor.RangedClickPlayerInteractable => _pushActivatable.RangedClickInteractionModule;
        ICollidePlayerInteratable ICollidePlayerInteratableImplementor.CollidePlayerInteratable => _pushActivatable.ColliderInteractionModule;
        #endregion

        private void OnEnable()
        {
            string id = "Activtable-" + gameObject.name; 
            _pushActivatable = PushActivatableFactory.Create(_config, _state, id);
        }

        private void FixedUpdate()
        {
            _pushActivatable.HandleFixedUpdate();
        }

        private void OnDisable()
        {
            _pushActivatable.TearDown();
            _pushActivatable = null;
        }

    }

    public static class PushActivatableFactory
    {
        public static PushActivatable Create(PushActivatableConfig config, ViRSESerializable state, string id)
        {
            //TODO - DON'T INJECT STATE! But maybe do inject the WorldStateModulesContainer , or have the state module have its own factory 
            SingleInteractorActivatableStateModule stateModule = new(state, config.StateConfig, id, ViRSECoreServiceLocator.Instance.WorldStateModulesContainer);
            GeneralInteractionModule GeneralInteractionModule = new(config.GeneralInteractionConfig);
            RangedClickInteractionModule RangedClickInteractionModule = new(config.RangedInteractionConfig);
            ColliderInteractionModule ColliderInteractionModule = new();

            return new PushActivatable(stateModule, GeneralInteractionModule, RangedClickInteractionModule, ColliderInteractionModule);
        }
    }

    public class PushActivatable
    {
        #region Modules
        public SingleInteractorActivatableStateModule StateModule { get; private set; }
        public GeneralInteractionModule GeneralInteractionModule { get; private set; }
        public RangedClickInteractionModule RangedClickInteractionModule { get; private set; }
        public ColliderInteractionModule ColliderInteractionModule { get; private set; }
        #endregion

        public PushActivatable(
            SingleInteractorActivatableStateModule stateModule,
            GeneralInteractionModule generalInteractionModule,
            RangedClickInteractionModule rangedClickInteractionModule,
            ColliderInteractionModule colliderInteractionModule)
        {
            StateModule = stateModule;
            GeneralInteractionModule = generalInteractionModule;
            RangedClickInteractionModule = rangedClickInteractionModule;
            ColliderInteractionModule = colliderInteractionModule;

            RangedClickInteractionModule.OnClickDown += HandleInteract;
            ColliderInteractionModule.OnCollideEnter += HandleInteract;
        }

        public void HandleFixedUpdate()
        {
            StateModule.HandleFixedUpdate();
        }

        private void HandleInteract(InteractorID interactorID)
        {
            //Could live in interactor, call InvertState on state module, which would emit an event to the network module "OnStateChanged"
            StateModule.InvertState(interactorID);
        }

        public void TearDown() 
        {
            StateModule.TearDown();
        }
    }
}
