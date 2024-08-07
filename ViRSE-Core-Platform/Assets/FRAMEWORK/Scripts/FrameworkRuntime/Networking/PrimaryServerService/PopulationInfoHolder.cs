using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;


namespace ViRSE.FrameworkRuntime
{
    //public class PopulationInfoHolder 
    //{
    //    public ushort LocalClientID { get; }
    //    public string LocalClientInstanceCode { get; }

    //    public ClientInfo LocalClientInfo => LocalInstanceInfo.ClientInfos[LocalClientID];
    //    public InstanceInfo LocalInstanceInfo => PopulationInfo.InstanceInfos[LocalClientInstanceCode];
    //    public PopulationInfo PopulationInfo { get; set; }
    //    public PopulationInfoHolder(ushort localClientID, string localClientInstanceCode, PopulationInfo populationInfo)
    //    {
    //        LocalClientID = localClientID;
    //        LocalClientInstanceCode = localClientInstanceCode;
    //        PopulationInfo = populationInfo;
    //    }
    //}

    public static class Environment //TODO move
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
