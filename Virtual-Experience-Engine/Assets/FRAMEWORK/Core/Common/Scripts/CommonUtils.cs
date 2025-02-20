using System.Collections.Generic;
using UnityEngine;

namespace VE2.Core.Common
{
    public static class CommonUtils
    {
        public static List<Material> GetAvatarColorMaterialsForGameObject(GameObject go) //TODO, move to player?
        {
            List<Material> colorMaterials = new();

            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
            {
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    if (renderer.materials[i].name.Contains("V_AvatarPrimary"))
                        colorMaterials.Add(renderer.materials[i]);
                }
            }

            return colorMaterials;
        }
    }
}
