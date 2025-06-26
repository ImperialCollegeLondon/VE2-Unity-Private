using System;
using UnityEngine;

    internal interface IGameObjectIDWrapper
    {
        public bool HasBeenSetup { get; }
        string ID { get; }
    }

    // internal class GameObjectIDWrapper : IGameObjectIDWrapper
    // {
    //     public string ID => $"{_prefix}-{_gameObject.name.ToString()}";

    //     private readonly GameObject _gameObject;
    //     private readonly string _prefix;

    //     public GameObjectIDWrapper(GameObject gameObject, string prefix)
    //     {
    //         _gameObject = gameObject;
    //         _prefix = prefix;
    //     }
    // }

    [Serializable]
    internal class GameObjectIDWrapper : IGameObjectIDWrapper
    {
        [SerializeField] public bool HasBeenSetup { get; set; } = false;
        [SerializeField] public string ID { get; set; } = null;
        public GameObjectIDWrapper() { }
}
