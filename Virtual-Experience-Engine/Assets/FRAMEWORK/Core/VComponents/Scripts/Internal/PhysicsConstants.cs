using UnityEngine;

//[CreateAssetMenu(fileName = "NewPhysicsConstants", menuName = "PhysicsConstants")]
namespace VE2.Core.VComponents.Internal
{
    internal class PhysicsConstants : ScriptableObject
    {
        [SerializeField] public float DefaultMaxVelocity = 10;
        [SerializeField] public float DefaultMaxAngularVelocity = 10;
        [SerializeField] public float VelocityScale = 0.35f;
        [SerializeField] public float VelocityDamping = 0.45f;
        [SerializeField] public float AngularVelocityScale = 0.35f;
        [SerializeField] public float AngularVelocityDamping = 0.45f;
    }
}
