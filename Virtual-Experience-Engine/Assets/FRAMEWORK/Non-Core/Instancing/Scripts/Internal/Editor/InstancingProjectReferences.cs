#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;

namespace VE2.NonCore.Instancing.Internal
{
    [CreateAssetMenu(fileName = "InstancingProjectReferences", menuName = "Scriptable Objects/InstancingProjectReferences")]
    internal class InstancingProjectReferences : ScriptableObject
    {
        public DefaultAsset LocalServerExecutable => _localServerExecutable;
        [SerializeField] private DefaultAsset _localServerExecutable;
    }
}

#endif