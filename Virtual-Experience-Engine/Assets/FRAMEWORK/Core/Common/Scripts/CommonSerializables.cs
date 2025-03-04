using System;
using System.IO;

#if UNITY_EDITOR
using UnityEngine;
#endif

namespace VE2.Core.Common 
{
    public class CommonSerializables
    {
        [Serializable]
        public abstract class VE2Serializable
        {
            public byte[] Bytes { get => ConvertToBytes(); set => PopulateFromBytes(value); }

            public VE2Serializable() { }

            public VE2Serializable(byte[] bytes)
            {
                PopulateFromBytes(bytes);
            }

            protected abstract byte[] ConvertToBytes();

            protected abstract void PopulateFromBytes(byte[] bytes);
        }
    }
}