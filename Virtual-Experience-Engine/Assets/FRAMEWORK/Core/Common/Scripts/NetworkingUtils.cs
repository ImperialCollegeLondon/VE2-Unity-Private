using System;
using static VE2.Common.CommonSerializables;

namespace VE2.Common //TODO break into different files
{
    //TODO: review if we should be coupling WorldStateModule and PlayerStateModule like this... they _are_ different things, handled by exclusively different systems 
    public interface IBaseStateModule 
    {
        public VE2Serializable State { get; }
        public TransmissionProtocol TransmissionProtocol { get; }
        public float TransmissionFrequency { get; }

        //TODO - could put config and wiring in here
    }

    public enum TransmissionProtocol //TODO - move
    {
        UDP,
        TCP
    }
}
