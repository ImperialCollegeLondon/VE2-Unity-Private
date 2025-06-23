#if UNITY_EDITOR
using Toolbox.Editor;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using VE2.Core.VComponents.Integration;

namespace VE2.Core.VComponents.Internal
{
    internal class AutoFixGameObjectClashes
    {
        // Types we have direct access to
        private static readonly Type[] VComponentTypes = new Type[]
        {
                typeof(V_ToggleActivatable),
                typeof(V_HoldActivatable),
                typeof(V_FreeGrabbable),
                typeof(V_PressurePlate),
                typeof(V_RotationalAdjustable),
                typeof(V_LinearAdjustable),
                typeof(V_HandheldAdjustable),
                typeof(V_CustomInfoPoint),
                typeof(InfoPointTriggerAnimationHandler),
                typeof(V_HandheldActivatable),
        };

        // Types we only know by name (string)
        private static readonly string[] VComponentTypeNames = new string[]
        {
                "V_RigidbodySyncable",
                "V_NetworkObject",
                "V_InstantMessageHandler"
        };

        // Matches "Name" or "Name2", "Name3", etc.
        private static readonly Regex numberedSuffixRegex = new Regex(@"^(.*?)(\d+)?$", RegexOptions.Compiled);

        [MenuItem("VE2/Fix GameObject Name Clashes", priority = 2)]
        private static void FixGameObjectNameClashes()
        {
            int totalRenamed = 0;

            // Process known types
            foreach (var type in VComponentTypes)
            {
                totalRenamed += FixClashesForType(type);
            }

            // Process types by name
            foreach (var typeName in VComponentTypeNames)
            {
                var type = FindTypeByName(typeName);
                if (type != null)
                {
                    totalRenamed += FixClashesForType(type);
                }
                else
                {
                    Debug.LogWarning($"[VE2] Could not find type '{typeName}' in loaded assemblies. Skipping.");
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

        private static int FixClashesForType(Type type)
        {
            int totalRenamed = 0;
            var components = GameObject.FindObjectsOfType(type, true).Cast<Component>().ToList();

            // Group by base name (without numeric suffix)
            var baseNameGroups = components
                .GroupBy(c => GetBaseNameAndNumber(c.gameObject.name).baseName);

            foreach (var group in baseNameGroups)
            {
                // Build a map of used numbers for this base name
                var usedNumbers = new HashSet<int>();
                var nameToComponent = new Dictionary<string, Component>();
                foreach (var comp in group)
                {
                    var (baseName, number) = GetBaseNameAndNumber(comp.gameObject.name);
                    usedNumbers.Add(number);
                    nameToComponent[comp.gameObject.name] = comp;
                }

                // If all names are unique, skip
                if (group.Count() == usedNumbers.Count)
                    continue;

                // Sort by (number, then name) for deterministic renaming
                var sorted = group
                    .Select(c => new { comp = c, info = GetBaseNameAndNumber(c.gameObject.name) })
                    .OrderBy(x => x.info.number)
                    .ThenBy(x => x.comp.gameObject.name)
                    .ToList();

                // Assign names: keep unique ones, only rename duplicates
                var assignedNames = new HashSet<string>();
                foreach (var entry in sorted)
                {
                    string desiredName;
                    var (baseName, number) = entry.info;

                    // Try to keep the current name if it's unique and not already assigned
                    if (!assignedNames.Contains(entry.comp.gameObject.name) &&
                        !assignedNames.Contains(baseName + (number > 0 ? number.ToString() : "")))
                    {
                        desiredName = entry.comp.gameObject.name;
                    }
                    else
                    {
                        // Find the next available number >= 2
                        int candidate = 1;
                        do
                        {
                            candidate++;
                            desiredName = baseName + (candidate == 1 ? "" : candidate.ToString());
                        } while (assignedNames.Contains(desiredName) || nameToComponent.ContainsKey(desiredName));
                    }

                    if (entry.comp.gameObject.name != desiredName)
                    {
                        Undo.RecordObject(entry.comp.gameObject, "Rename GameObject to avoid clash");
                        entry.comp.gameObject.name = desiredName;
                        totalRenamed++;
                    }
                    assignedNames.Add(desiredName);
                }
            }
            return totalRenamed;
        }

        // Returns (baseName, number). "PushButtonToggle" => ("PushButtonToggle", 0), "PushButtonToggle2" => ("PushButtonToggle", 2)
        private static (string baseName, int number) GetBaseNameAndNumber(string name)
        {
            var match = numberedSuffixRegex.Match(name);
            if (match.Success)
            {
                string baseName = match.Groups[1].Value;
                string numberStr = match.Groups[2].Value;
                int number = 0;
                if (!string.IsNullOrEmpty(numberStr) && int.TryParse(numberStr, out int parsed))
                    number = parsed;
                return (baseName, number);
            }
            return (name, 0);
        }

        // Finds a type by name, searching all loaded assemblies if necessary
        private static Type FindTypeByName(string typeName)
        {
            // Try Type.GetType first (works for fully qualified names)
            var type = Type.GetType(typeName);
            if (type != null)
                return type;

            // Search all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                    return type;
                // Try without namespace if not found
                type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
#endif