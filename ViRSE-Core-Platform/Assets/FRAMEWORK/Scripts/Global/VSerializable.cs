using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE
{
    public abstract class VSerializable
    {
        public byte[] Bytes { get => ConvertToBytes(); set => PopulateFromBytes(value); }

        public VSerializable() { }

        public VSerializable(byte[] bytes)
        {
            Bytes = bytes;
        }

        protected abstract byte[] ConvertToBytes();

        protected abstract void PopulateFromBytes(byte[] bytes);
    }
}
