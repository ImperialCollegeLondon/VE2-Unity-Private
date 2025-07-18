using System;
using System.IO;
using UnityEngine;

namespace VE2.Common.Shared
{
    internal class CommonSerializables
    {
        [Serializable]
        internal abstract class VE2Serializable
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