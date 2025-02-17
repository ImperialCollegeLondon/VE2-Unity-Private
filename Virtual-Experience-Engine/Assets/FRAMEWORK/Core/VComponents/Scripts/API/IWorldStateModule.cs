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

    //Do we really want to remove this?
    //Means the SyncService MUST be running by the time we access it 
    //Means the instance server settings must be good to go right away, i.e, they get read in from the hub 
    //Would we ever want to be able to do the whole flow in the editor? Of connecting to the platform, and having it point us to the server?
    // I guess not? If we really need to test that, go in from intro scene/sample scene
    // Maybe let's keep using this container for now? May be good to decouple these things regardless?? 

    // public class WorldStateModulesContainer : BaseStateModuleContainer
    // {
    //     private List<IWorldStateModule> _worldstateSyncableModules = new();
    //     public IReadOnlyList<IWorldStateModule> WorldstateSyncableModules => _worldstateSyncableModules.AsReadOnly();
    //     public event Action<IWorldStateModule> OnWorldStateModuleRegistered;
    //     public event Action<IWorldStateModule> OnWorldStateModuleDeregistered;

    //     public override void RegisterStateModule(IBaseStateModule moduleBase)
    //     {
    //         IWorldStateModule module = (IWorldStateModule)moduleBase;
    //         _worldstateSyncableModules.Add(module);
    //         OnWorldStateModuleRegistered?.Invoke(module);
    //     }

    //     public override void DeregisterStateModule(IBaseStateModule moduleBase)
    //     {
    //         IWorldStateModule module = (IWorldStateModule)moduleBase;
    //         _worldstateSyncableModules.Remove(module);
    //         OnWorldStateModuleDeregistered?.Invoke(module);
    //     }

    //     public override void Reset() => _worldstateSyncableModules.Clear();
    // }

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


