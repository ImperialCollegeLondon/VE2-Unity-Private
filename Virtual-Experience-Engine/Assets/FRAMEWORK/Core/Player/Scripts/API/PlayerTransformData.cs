using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static VE2.Common.Shared.CommonSerializables;

namespace VE2.Core.Player.API
{
    //TODO, we know the player will always be rotated to be level with the floor 
    //So that means we can actually just transmit a single float for the root rotation angle
    [Serializable]
    internal class PlayerTransformData : VE2Serializable
    {
        public bool IsVRMode;

        public Vector3 RootPosition;
        public Quaternion RootRotation;
        public float VerticalOffset;
        public Vector3 HeadLocalPosition { get; private set; }
        public Quaternion HeadLocalRotation { get; private set; }
        public Vector3 Hand2DLocalPosition { get; private set; }
        public Quaternion Hand2DLocalRotation { get; private set; }
        public Vector3 HandVRLeftLocalPosition { get; private set; }
        public Quaternion HandVRLeftLocalRotation { get; private set; }
        public Vector3 HandVRRightLocalPosition { get; private set; }
        public Quaternion HandVRRightLocalRotation { get; private set; }
        public List<string> HeldActivatableIds2D { get; private set; }
        public List<string> HeldActivatableIdsVRLeft { get; private set; }
        public List<string> HeldActivatableIdsVRRight { get; private set; }
        public List<string> HeldActivatableIdsFeet { get; private set; }

        public PlayerTransformData(byte[] bytes) : base(bytes) { }

        public PlayerTransformData() : base() { }

        public PlayerTransformData(bool IsVRMode, Vector3 rootPosition, Quaternion rootRotation, float verticalOffset, Vector3 headPosition, Quaternion headRotation, Vector3 hand2DPosition, Quaternion hand2DRotation, List<string> activatableIDs2D, List<string> activatableIDsFeet)
        {
            this.IsVRMode = IsVRMode;
            if (IsVRMode)
                Debug.LogError("PlayerTransformData created with VR mode, but only given a single hand transform - perhaps you called the wrong constructor? " + Environment.StackTrace);

            RootPosition = rootPosition;
            RootRotation = rootRotation;
            VerticalOffset = verticalOffset;
            HeadLocalPosition = headPosition;
            HeadLocalRotation = headRotation;
            Hand2DLocalPosition = hand2DPosition;
            Hand2DLocalRotation = hand2DRotation;
            HeldActivatableIds2D = activatableIDs2D;
            HeldActivatableIdsFeet = activatableIDsFeet;
        }

        public PlayerTransformData(bool IsVRMode, Vector3 rootPosition, Quaternion rootRotation, float verticalOffset, Vector3 headPosition, Quaternion headRotation, Vector3 handVRLeftPosition, Quaternion handVRLeftRotation, Vector3 handVRRightPosition, Quaternion handVRRightRotation, List<string> activatableIDsVRLeft, List<string> activatableIDsVRRight, List<string> activatableIDsFeet)
        {
            this.IsVRMode = IsVRMode;
            if (!IsVRMode)
                Debug.LogError("PlayerTransformData created with 2D mode, but only given two hand transforms - perhaps you called the wrong constructor? " + Environment.StackTrace);

            RootPosition = rootPosition;
            RootRotation = rootRotation;
            VerticalOffset = verticalOffset;
            HeadLocalPosition = headPosition;
            HeadLocalRotation = headRotation;
            HandVRLeftLocalPosition = handVRLeftPosition;
            HandVRLeftLocalRotation = handVRLeftRotation;
            HandVRRightLocalPosition = handVRRightPosition;
            HandVRRightLocalRotation = handVRRightRotation;
            HeldActivatableIdsVRLeft = activatableIDsVRLeft;
            HeldActivatableIdsVRRight = activatableIDsVRRight;
            HeldActivatableIdsFeet = activatableIDsFeet;
        }


        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(IsVRMode);

            writer.Write(RootPosition.x);
            writer.Write(RootPosition.y);
            writer.Write(RootPosition.z);

            writer.Write(RootRotation.x);
            writer.Write(RootRotation.y);
            writer.Write(RootRotation.z);
            writer.Write(RootRotation.w);

            writer.Write(Mathf.FloatToHalf(VerticalOffset));

            writer.Write(HeadLocalPosition.x);
            writer.Write(HeadLocalPosition.y);
            writer.Write(HeadLocalPosition.z);

            writer.Write(HeadLocalRotation.x);
            writer.Write(HeadLocalRotation.y);
            writer.Write(HeadLocalRotation.z);
            writer.Write(HeadLocalRotation.w);

            if (!IsVRMode)
            {
                writer.Write(Hand2DLocalPosition.x);
                writer.Write(Hand2DLocalPosition.y);
                writer.Write(Hand2DLocalPosition.z);

                writer.Write(Hand2DLocalRotation.x);
                writer.Write(Hand2DLocalRotation.y);
                writer.Write(Hand2DLocalRotation.z);
                writer.Write(Hand2DLocalRotation.w);

                if(HeldActivatableIds2D == null)
                    HeldActivatableIds2D = new List<string>();

                foreach (string activatableID in HeldActivatableIds2D)
                    writer.Write(activatableID);
            }
            else
            {
                writer.Write(HandVRLeftLocalPosition.x);
                writer.Write(HandVRLeftLocalPosition.y);
                writer.Write(HandVRLeftLocalPosition.z);

                writer.Write(HandVRLeftLocalRotation.x);
                writer.Write(HandVRLeftLocalRotation.y);
                writer.Write(HandVRLeftLocalRotation.z);
                writer.Write(HandVRLeftLocalRotation.w);

                writer.Write(HandVRRightLocalPosition.x);
                writer.Write(HandVRRightLocalPosition.y);
                writer.Write(HandVRRightLocalPosition.z);

                writer.Write(HandVRRightLocalRotation.x);
                writer.Write(HandVRRightLocalRotation.y);
                writer.Write(HandVRRightLocalRotation.z);
                writer.Write(HandVRRightLocalRotation.w);

                if(HeldActivatableIdsVRLeft == null)
                    HeldActivatableIdsVRLeft = new List<string>();

                if(HeldActivatableIdsVRRight == null)
                    HeldActivatableIdsVRRight = new List<string>();

                foreach (string activatableID in HeldActivatableIdsVRLeft)
                    writer.Write(activatableID);

                foreach (string activatableID in HeldActivatableIdsVRRight)
                    writer.Write(activatableID);
            }

            foreach (string activatableID in HeldActivatableIdsFeet)
                writer.Write(activatableID);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            IsVRMode = reader.ReadBoolean();
            RootPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            RootRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            VerticalOffset = Mathf.HalfToFloat(reader.ReadUInt16());
            HeadLocalPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            HeadLocalRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            if (!IsVRMode)
            {
                Hand2DLocalPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                Hand2DLocalRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                HeldActivatableIds2D = new();
                while (stream.Position < stream.Length)
                    HeldActivatableIds2D.Add(reader.ReadString());
            }
            else
            {
                HandVRLeftLocalPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                HandVRLeftLocalRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                HandVRRightLocalPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                HandVRRightLocalRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                HeldActivatableIdsVRLeft = new();
                while (stream.Position < stream.Length)
                    HeldActivatableIdsVRLeft.Add(reader.ReadString());

                HeldActivatableIdsVRRight = new();
                while (stream.Position < stream.Length)
                    HeldActivatableIdsVRRight.Add(reader.ReadString());

            }

            HeldActivatableIdsFeet = new();
            while (stream.Position < stream.Length)
                HeldActivatableIdsFeet.Add(reader.ReadString());
        }
    }
}
