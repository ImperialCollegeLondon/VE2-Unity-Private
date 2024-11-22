using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common;
using VE2.Core.Common;
using VE2.NonCore.Instancing.VComponents.NonInteractableInterfaces;
using static VE2.Common.CommonSerializables;

namespace VE2.NonCore.Instancing.VComponents.Internal
{
    [Serializable]
    public class RigidbodySyncableStateConfig : BaseStateConfig { }

    internal class RigidbodySyncableStateModule : BaseWorldStateModule, IRigidbodySyncableStateModule
    {

        private RigidbodySyncableState _state => (RigidbodySyncableState)State;
        private RigidbodySyncableStateConfig _config => (RigidbodySyncableStateConfig)Config;

        public RigidbodySyncableStateModule(VE2Serializable state, BaseStateConfig config, string id, WorldStateModulesContainer worldStateModulesContainer) : base(state, config, id, worldStateModulesContainer) { }


        protected override void UpdateBytes(byte[] newBytes)
        {
            State.Bytes = newBytes;
        }
    }

    [Serializable]
    public class RigidbodySyncableState : VE2Serializable
    {

        public RigidbodySyncableState() { }

        protected override byte[] ConvertToBytes()
        {
            throw new NotImplementedException();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            throw new NotImplementedException();
        }
    }
}
