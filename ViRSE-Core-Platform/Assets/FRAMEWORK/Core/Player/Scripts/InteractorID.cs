using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VE2.Core.Common;

namespace VE2
{
    public class InteractorID //TODO - PlayerRig needs this too, maybe move into some shared assembly, where both PluginAPI and PlayerRigAPI can see it?
    {
        public ushort ClientID { get; }
        public InteractorType InteractorType { get; }

        public InteractorID(ushort clientID, InteractorType interactorType)
        {
            ClientID = clientID;
            InteractorType = interactorType;
        }

        public override string ToString()
        {
            return $"Client{ClientID}-{InteractorType}";
        }

        public override bool Equals(object obj)
        {
            return obj is InteractorID iD &&
                   ClientID == iD.ClientID &&
                   InteractorType == iD.InteractorType;
        }
    }
}