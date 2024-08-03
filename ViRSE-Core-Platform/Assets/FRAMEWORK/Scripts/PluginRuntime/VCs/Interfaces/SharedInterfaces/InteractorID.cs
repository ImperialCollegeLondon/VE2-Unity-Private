using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE
{
    public class InteractorID //TODO - PlayerRig needs this too, maybe move into some shared assembly, where both PluginAPI and PlayerRigAPI can see it?
    {
        public int ClientID { get; }
        public InteractorType InteractorType { get; }

        public InteractorID(int clientID, InteractorType interactorType)
        {
            ClientID = clientID;
            InteractorType = interactorType;
        }

        public override string ToString()
        {
            return $"Client{ClientID}-{InteractorType}";
        }
    }

    public enum InteractorType
    {
        TwoD,
        VRRight,
        VRLeft,
        Feet
    }
}