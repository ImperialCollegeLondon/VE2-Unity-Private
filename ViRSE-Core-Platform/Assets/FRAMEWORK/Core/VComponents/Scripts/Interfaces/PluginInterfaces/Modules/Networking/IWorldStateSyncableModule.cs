using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ViRSE.PluginRuntime.VComponents
{
    public interface IWorldStateSyncableModuleImplementor
    {
        //protected IWorldStateSyncableModule _module { get; }
        //public float SyncFrequency { get => _module.SyncFrequency; set => _module.SyncFrequency = value; }
        //public TransmissionProtocol TransmissionProtocol { get => _module.TransmissionProtocol; set => _module.TransmissionProtocol = value; }
    }

    public interface IWorldStateSyncableModule
    {
        //public float SyncFrequency { get; set; }
        //public TransmissionProtocol TransmissionProtocol { get; set; }
    }
}