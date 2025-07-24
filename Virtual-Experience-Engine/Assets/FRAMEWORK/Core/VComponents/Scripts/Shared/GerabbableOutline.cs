using System.Linq;
using UnityEngine;

namespace VE2.Core.VComponents.Shared
{
    internal class GrabbableOutline
    {
        private Material outlineMaskMaterial;
        private Material outlineFillMaterial;

        public GrabbableOutline(Material outlineMask, Material outlineFill)
        {
            outlineMaskMaterial = outlineMask;
            outlineFillMaterial = outlineFill;
        }

        public void HandleOnEnable(Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0) return;

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                if (renderer.gameObject.GetComponent<LineRenderer>() != null)
                    continue;

                //Trying to operate on a GO that has a VFX on will crash Unity!
                //VFX should be put on a child gameobject instead
                if (renderer.gameObject.GetComponent<UnityEngine.VFX.VisualEffect>() != null)
                    continue;

                var materials = renderer.sharedMaterials.ToList();

                materials.Add(outlineMaskMaterial);
                materials.Add(outlineFillMaterial);

                renderer.materials = materials.ToArray();
            }
        }
    }
}