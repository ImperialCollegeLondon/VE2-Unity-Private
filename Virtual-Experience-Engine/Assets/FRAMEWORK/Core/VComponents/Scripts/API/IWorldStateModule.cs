using System;
using System.Collections.Generic;
using VE2.Core.Common;
using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.Common //TODO break into different files
{
    public interface IWorldStateModule : IBaseStateModule
    {
        public string ID { get; }
        public byte[] StateAsBytes { get; set; }
    }

    public class WorldStateModulesContainer : BaseStateModuleContainer
    {
        private List<IWorldStateModule> _worldstateSyncableModules = new();
        public IReadOnlyList<IWorldStateModule> WorldstateSyncableModules => _worldstateSyncableModules.AsReadOnly();
        public event Action<IWorldStateModule> OnWorldStateModuleRegistered;
        public event Action<IWorldStateModule> OnWorldStateModuleDeregistered;

        public override void RegisterStateModule(IBaseStateModule moduleBase)
        {
            IWorldStateModule module = (IWorldStateModule)moduleBase;
            _worldstateSyncableModules.Add(module);
            OnWorldStateModuleRegistered?.Invoke(module);
        }

        public override void DeregisterStateModule(IBaseStateModule moduleBase)
        {
            IWorldStateModule module = (IWorldStateModule)moduleBase;
            _worldstateSyncableModules.Remove(module);
            OnWorldStateModuleDeregistered?.Invoke(module);
        }

        public override void Reset() => _worldstateSyncableModules.Clear();
    }

    public class InteractorContainer
    {
        private Dictionary<string, IInteractor> _interactors = new();
        public IReadOnlyDictionary<string, IInteractor> Interactors => _interactors;

        public void RegisterInteractor(string interactorID, IInteractor interactor)
        {
            _interactors[interactorID] = interactor;
        }

        public void DeregisterInteractor(string interactorID)
        {
            _interactors.Remove(interactorID);
        }

        public void Reset() => _interactors.Clear();
    }
}


