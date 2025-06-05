using System;
using System.Collections.Generic;

//Needs to be called by hub ui 
//Maybe plugins need access to return to hub? 

//Used by hub UI, and platform player browser UI
//TODO: Should be IPlatformIntegration?

namespace VE2.NonCore.Platform.API
{
    public interface IPlatformService //TODO, maybe not all of these should live in the same interface?
    {
        public bool IsConnectedToServer { get; }
        public event Action OnConnectedToServer;
        public string CurrentInstanceNumber { get; }
        public string CurrentWorldName { get; }
        public event Action OnLeavingInstance;
    }

    /*
    *  One interface that faces the platform integration package that gets imported by customers 
    *  Another interface that faces the private platform stuff, the same package that the PlatformService lives in, is meant to provide available worlds, and global info 
    * 
    */
}
