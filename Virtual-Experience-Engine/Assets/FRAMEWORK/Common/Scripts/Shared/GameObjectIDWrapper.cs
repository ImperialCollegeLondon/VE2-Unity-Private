using System;
using UnityEngine;

    internal interface IGameObjectIDWrapper
    {
        public bool HasBeenSetup { get; }
        string ID { get; }
    }

    [Serializable]
    internal class GameObjectIDWrapper : IGameObjectIDWrapper
    {
        [SerializeField] public bool HasBeenSetup { get; set; } = false;
        [SerializeField] public string ID { get; set; } = null;
        public GameObjectIDWrapper() { }
}
