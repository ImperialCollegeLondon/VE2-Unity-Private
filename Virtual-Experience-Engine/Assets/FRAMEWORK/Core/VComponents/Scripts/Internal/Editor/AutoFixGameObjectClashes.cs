#if UNITY_EDITOR
using Toolbox.Editor;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace VE2.Core.VComponents.Internal
{
    internal class AutoFixGameObjectClashes
    {
        // Add your VComponent types here  
        private static readonly Type[] VComponentTypes = new Type[]
        {
                typeof(V_ToggleActivatable),
                typeof(V_HoldActivatable),
                typeof(V_FreeGrabbable),
                typeof(V_PressurePlate),
                typeof(V_RotationalAdjustable),
                typeof(V_LinearAdjustable),
                typeof(V_HandheldAdjustable)
        };

        // Regex to match Unity's duplicate naming: "Name", "Name (1)", "Name (2)", etc.
        private static readonly Regex duplicateRegex = new Regex(@"^(.*?)( \(\d+\))?$", RegexOptions.Compiled);

        [MenuItem("VE2/Fix GameObject Name Clashes")]
        private static void FixGameObjectNameClashes()
        {
            int totalRenamed = 0;
            foreach (var type in VComponentTypes)
            {
                var components = GameObject.FindObjectsOfType(type, true);

                // Build a set of all names already used for this type
                HashSet<string> usedNames = new HashSet<string>(components.Cast<Component>().Select(c => c.gameObject.name));

                // Group by normalized name (removing Unity's " (n)" suffix)
                var nameGroups = components.Cast<Component>()
                    .GroupBy(c => GetBaseName(c.gameObject.name))
                    .Where(g => g.Count() > 1);

                foreach (var group in nameGroups)
                {
                    string baseName = group.Key;
                    int nextSuffix = 1;
                    foreach (var comp in group)
                    {
                        string newName = baseName;
                        // Find the next available unique name
                        while (usedNames.Contains(newName))
                        {
                            nextSuffix++;
                            newName = baseName + nextSuffix;
                        }
                        if (comp.gameObject.name != newName)
                        {
                            Undo.RecordObject(comp.gameObject, "Rename GameObject to avoid clash");
                            comp.gameObject.name = newName;
                            totalRenamed++;
                            usedNames.Add(newName);
                        }
                        else
                        {
                            // Ensure the current name is marked as used
                            usedNames.Add(newName);
                        }
                    }
                }
            }

            if (totalRenamed > 0)
            {
                Debug.Log($"[VE2] Fixed {totalRenamed} GameObject name clash(es).");
            }
            else
            {
                Debug.Log("[VE2] No GameObject name clashes found.");
            }
        }

        // Removes Unity's " (n)" suffix from names
        private static string GetBaseName(string name)
        {
            var match = duplicateRegex.Match(name);
            return match.Success ? match.Groups[1].Value : name;
        }
    }
}
#endif