
//#if UnityEditor
using System.IO;
using UnityEngine;

namespace VE2.NonCore.Instancing.Internal
{
    [CreateAssetMenu(fileName = "InstancingProjectReferences", menuName = "Scriptable Objects/InstancingProjectReferences")]
    public class InstancingProjectReferences : ScriptableObject
    {
        public UnityEditor.DefaultAsset LocalServerExecutable => _localServerExecutable;
        [SerializeField] private UnityEditor.DefaultAsset _localServerExecutable;
    }
}

//#endif