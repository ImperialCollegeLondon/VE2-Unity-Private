using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace ViRSE.FrameworkRuntime
{
    public class PopulationInfoWrapper //TODO - would rather this be a static utils class?
    {
        public ushort LocalClientID { get; }
        public string LocalClientInstanceCode { get; }

        public ClientInfo LocalClientInfo => LocalInstanceInfo.ClientInfos[LocalClientID];
        public InstanceInfo LocalInstanceInfo => PopulationInfo.InstanceInfos[LocalClientInstanceCode];
        public PopulationInfo PopulationInfo { get; set; }
        public PopulationInfoWrapper(ushort localClientID, string localClientInstanceCode, PopulationInfo populationInfo)
        {
            LocalClientID = localClientID;
            LocalClientInstanceCode = localClientInstanceCode;
            PopulationInfo = populationInfo;
        }
    }

    /*Where do we want all these to live 
     * For instance syncing stuff, it lives in the actual state object, and is never serialized by the comms layer
     * Maybe that makes sense, given that its part of the PluginRuntime 
     * For anything framework side though... we could have a big "DarkRift Messages" class, that takes the actual object and is in charge of serializing it 
     * Urgh, the thing is, it IS nice to have the instructions for serialization as a neccessary PART of the class 
     * 
     */

    public static class Environment
    {
        public static int frameworkVersion;

        public enum Type
        {
            Build,
            CorePlatform,
            Server,
            WorldCreation
        }

        public static readonly Type type = Type.CorePlatform;
    }
}
