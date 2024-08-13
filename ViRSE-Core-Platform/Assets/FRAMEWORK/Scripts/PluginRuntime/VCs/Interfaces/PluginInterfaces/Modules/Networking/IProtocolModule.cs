using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.PluginRuntime.VComponents
{
    public interface IProtocolModuleImplementor
    {
        protected IProtocolModule _module { get; }
        //public TransmissionProtocol TransmissionProtocol { get => _module.TransmissionProtocol; set => _module.TransmissionProtocol = value; }
    }

    public interface IProtocolModule
    {
        //public TransmissionProtocol TransmissionProtocol { get; set; }
    }
}
