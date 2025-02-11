using System;
using static VE2.Common.CommonSerializables;

namespace VE2.Common //TODO break into different files
{
    public interface IInstanceService
    {
        public abstract bool IsEnabled { get; }
        public abstract string GameObjectName { get; }
        public ushort LocalClientID { get; }
        public bool IsHost { get; }
        public bool IsConnectedToServer { get; }
        public event Action OnConnectedToInstance;

        //the plugin code always knows whether or not the VCs are actually present, 
        //Which means the VC interface lives in the actual VC
        //BUt even if the instrancing assemblies aren't present, Core needs a conceptaulisation of the instancing 

        //Lets have a partial class for the V_II? Eh, let's just do the wiring directly in the mono
    }
}
