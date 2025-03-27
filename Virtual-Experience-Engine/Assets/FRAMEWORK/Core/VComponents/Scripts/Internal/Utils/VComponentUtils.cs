using UnityEngine;

namespace VE2.Core.VComponents.Internal
{
    internal static class VComponentUtils
    {
        internal static void CreateCollider(GameObject gameObject)
        {
            Collider collider;
            if (gameObject.name.ToUpper().Contains("CUBE") || gameObject.name.ToUpper().Contains("BOX"))
                collider = gameObject.AddComponent<BoxCollider>();
            else if (gameObject.name.ToUpper().Contains("SPHERE") || gameObject.name.ToUpper().Contains("BALL"))
                collider =gameObject.AddComponent<SphereCollider>();
            else
            {
                collider = gameObject.AddComponent<MeshCollider>();
                ((MeshCollider)collider).convex = true;
            }

            collider.isTrigger = false;
        }
    }
}
