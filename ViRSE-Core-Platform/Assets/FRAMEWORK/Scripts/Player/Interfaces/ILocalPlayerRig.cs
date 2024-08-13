using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.FrameworkRuntime.LocalPlayerRig
{
    public interface ILocalPlayerRig
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }

        public void Initialize(UserSettings playerSettings);
    }
}

