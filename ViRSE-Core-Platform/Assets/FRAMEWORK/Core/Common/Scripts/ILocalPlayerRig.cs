using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViRSE.Core.Player;
using ViRSE.Core.Shared;

namespace ViRSE.Core.Player
{
    public interface ILocalPlayerRig
    {
        public Vector3 RootPosition { get; set; } //Will be on the floor
        public Quaternion RootRotation { get; set; } //Will be level with the floor
        public Vector3 HeadPosition { get; }  //The position of the camera
        public Quaternion HeadRotation { get;} //The rotation of the camera 

        //TODO - consider putting these in a separate interface
        public TransmissionProtocol TransmissionProtocol { get; }
        public float TransmissionFrequency { get; }
    }
}
