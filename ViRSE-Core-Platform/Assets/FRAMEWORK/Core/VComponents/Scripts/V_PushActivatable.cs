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


    public class V_PushActivatable : BaseStateHolder, IV_PushActivatable, IRangedClickPlayerInteractableIntegrator, ICollidePlayerInteratableImplementor
    {
        [SerializeField, HideLabel, IgnoreParent] private PushActivatableConfig _config = new(); 
        [SerializeField, HideInInspector] private SingleInteractorActivatableState _state = new();

        private PushActivatable _pushActivatable = null;
        public override BaseStateConfig BaseStateConfig => _config.StateConfig;

        #region Customer facing Interfaces
        IPushActivatable IV_PushActivatable._pushActivatableService => _pushActivatable;
        #endregion

        #region Player facing Interfaces
        IGeneralPlayerInteractableImplementor IGeneralPlayerInteractableIntegrator.GeneralPlayerInteractableImplementor => _pushActivatable;
        IRangedPlayerInteractableImplementor IRangedPlayerInteractableIntegrator.RangedPlayerInteractableImplementor => _pushActivatable;
        IRangedClickPlayerInteractableImplementor IRangedClickPlayerInteractableIntegrator.RangedClickPlayerInteractableImplementor => _pushActivatable;
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
            return new PushActivatable(config, state, id, ViRSECoreServiceLocator.Instance.WorldStateModulesContainer);
        }
    }

    public class PushActivatable : IRangedClickPlayerInteractableImplementor, IPushActivatable
    {
        #region Customer-facing Interfaces 
        ISingleInteractorActivatableStateModule ISingleInteractorActivatableStateModuleImplementor._stateModule => StateModule;
        IGeneralInteractionModule IGeneralInteractionModuleImplementor._module => GeneralInteractionModule;
        IRangedInteractionModule IRangedInteractionModuleImplementor._module => RangedClickInteractionModule;
        #endregion

        #region Player-facing Interfaces 
        IGeneralPlayerInteractable IGeneralPlayerInteractableImplementor.GeneralPlayerInteractable => GeneralInteractionModule;
        IRangedPlayerInteractable IRangedPlayerInteractableImplementor.RangedPlayerInteractable => RangedClickInteractionModule;
        IRangedClickPlayerInteractable IRangedClickPlayerInteractableImplementor.RangedClickPlayerInteractable => RangedClickInteractionModule;
        #endregion

        #region Modules
        public SingleInteractorActivatableStateModule StateModule { get; private set; }
        public GeneralInteractionModule GeneralInteractionModule { get; private set; }
        public RangedClickInteractionModule RangedClickInteractionModule { get; private set; }
        public ColliderInteractionModule ColliderInteractionModule { get; private set; }
        #endregion

        public PushActivatable(PushActivatableConfig config, ViRSESerializable state, string id, WorldStateModulesContainer worldStateModulesContainer)
        {
            StateModule = new(state, config.StateConfig, id, worldStateModulesContainer);
            GeneralInteractionModule = new(config.GeneralInteractionConfig);
            RangedClickInteractionModule = new(config.RangedInteractionConfig, config.GeneralInteractionConfig);
            ColliderInteractionModule = new();

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
