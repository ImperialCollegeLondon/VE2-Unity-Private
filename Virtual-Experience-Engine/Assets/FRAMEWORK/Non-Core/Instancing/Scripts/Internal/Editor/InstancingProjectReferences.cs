#if UnityEditor

using System.IO;
using UnityEditor;
using UnityEngine;

namespace VE2.NonCore.Instancing.Internal
{
    [CreateAssetMenu(fileName = "InstancingProjectReferences", menuName = "Scriptable Objects/InstancingProjectReferences")]
    public class InstancingProjectReferences : ScriptableObject
    {
        [SerializeField] private DefaultAsset localServerExecutable;
    }
}

#endif