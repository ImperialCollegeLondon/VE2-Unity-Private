#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace VE2.Common.Shared
{
    internal class VE2AutoAsmDef
    {
        private static readonly string[] RequiredReferences = new[]
        {
            "Unity.TextMeshPro",
            "Unity.InputSystem",
            "VE2.Common.API",
            "VE2.Common.Shared",
            "VE2.Core.VComponents.API",
            "VE2.Core.Player.API",
            "VE2.Core.UI.API",
            "VE2.NonCore.Instancing.API",
            "VE2.NonCore.Platform.API",
        };

        //[MenuItem("VE2/Create Plugin Assembly Definition", priority = -1000)]
        public static void CreateOrUpdateAsmdef()
        {
            string folderPath = "Assets/Scripts";
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Find existing .asmdef in folder
            string existingAsmdefPath = Directory.GetFiles(folderPath, "*.asmdef").FirstOrDefault();
            if (existingAsmdefPath != null)
            {
                Debug.Log($"Found existing asmdef at: {existingAsmdefPath}, checking references...");

                string existingJson = File.ReadAllText(existingAsmdefPath);
                List<string> currentRefs = ExtractReferences(existingJson);
                List<string> toAdd = RequiredReferences.Where(r => !currentRefs.Contains(r)).ToList();

                if (toAdd.Count == 0)
                {
                    Debug.Log("All required references already exist. No changes made.");
                    return;
                }

                currentRefs.AddRange(toAdd);
                string newJson = ReplaceReferences(existingJson, currentRefs);
                File.WriteAllText(existingAsmdefPath, newJson);
                AssetDatabase.Refresh();
                Debug.Log($"Updated asmdef at {existingAsmdefPath} with {toAdd.Count} new references.");
                return;
            }

            // Create a new .asmdef with a randomized name
            string asmdefName = GenerateRandomAsmdefName();
            string asmdefPath = Path.Combine(folderPath, asmdefName + ".asmdef");

            string referencesJson = string.Join(",\n        ", RequiredReferences.Select(r => $"\"{r}\""));
            string json = $@"{{
                ""name"": ""{asmdefName}"",
                ""references"": [
                    {referencesJson}
                ],
                ""includePlatforms"": [],
                ""excludePlatforms"": [],
                ""allowUnsafeCode"": false,
                ""overrideReferences"": false,
                ""precompiledReferences"": [],
                ""autoReferenced"": true,
                ""defineConstraints"": [],
                ""versionDefines"": [],
                ""noEngineReferences"": false
            }}";

            File.WriteAllText(asmdefPath, json);
            AssetDatabase.Refresh();
            Debug.Log("Created new .asmdef at: " + asmdefPath);
        }

        private static string GenerateRandomAsmdefName()
        {
            var adjective = Adjectives[Random.Range(0, Adjectives.Length)];
            var noun = Nouns[Random.Range(0, Nouns.Length)];
            var number = Random.Range(1000, 9999);
            return $"PluginAssembly_{adjective}{noun}{number}";
        }

        private static List<string> ExtractReferences(string json)
        {
            var lines = json.Split('\n');
            var refs = new List<string>();
            bool insideRefs = false;

            foreach (var line in lines)
            {
                if (line.Contains("\"references\""))
                    insideRefs = true;
                else if (insideRefs && line.Contains("]"))
                    break;
                else if (insideRefs)
                {
                    string trimmed = line.Trim().Trim(',', '"');
                    if (!string.IsNullOrEmpty(trimmed))
                        refs.Add(trimmed);
                }
            }

            return refs;
        }

        private static string ReplaceReferences(string originalJson, List<string> newReferences)
        {
            string newRefBlock = string.Join(",\n        ", newReferences.Select(r => $"\"{r}\""));
            int start = originalJson.IndexOf("\"references\"");
            if (start < 0) return originalJson;

            int arrayStart = originalJson.IndexOf("[", start);
            int arrayEnd = originalJson.IndexOf("]", arrayStart);
            if (arrayStart < 0 || arrayEnd < 0) return originalJson;

            string before = originalJson.Substring(0, arrayStart + 1);
            string after = originalJson.Substring(arrayEnd);

            return before + "\n        " + newRefBlock + after;
        }

        private static readonly string[] Adjectives = new string[]
        {
            "Stellar", "Quantum", "Cosmic", "Galactic", "Nebular", "Ionic", "Plasmic", "Fusional", "Warped", "Temporal",
            "Dark", "Luminous", "Ionized", "Gravitational", "Crystalline", "Chronal", "Spectral", "Solar", "Novaic", "Tachyonic",
            "Subspatial", "Dimensional", "Fractal", "Relativistic", "Zeropoint", "Voidal", "Frigid", "Radiant", "Entropic", "Hypercharged",
            "Ecliptic", "Exotic", "Eventual", "Geodesic", "Incandescent", "Infrared", "Inertial", "Iridescent", "Irradiated", "Keplerian",
            "Kinetic", "Lagrangian", "Lasered", "Magnetic", "Material", "Meteoric", "Multiversal", "Nanotized", "Neutronic", "Oblivious",
            "Orbital", "Parallaxed", "Photonic", "Phased", "Radiative", "Reactive", "Redshifted", "Rotational", "Scalar", "Seismic",
            "Shimmering", "Shockwaved", "Singular", "Solarian", "Spectrometric", "Spaghettified", "Starbound", "Subatomic", "Supersymmetric", "Synaptic",
            "Synthetic", "Telescopic", "TemporallyShifted", "Thermal", "Transdimensional", "Transwarp", "Ultraluminous", "Umbral", "Unstable", "Vectorial",
            "VelocityTuned", "Vibrant", "Volatile", "WavelengthScaled", "Wormholed", "Xenotic", "XrayEmitting", "Zenithal", "ZeroG", "ZerothLevel",
            "Anomalous", "Cryogenic", "Digitized", "Electromagnetic", "Ferrous", "Superluminal", "Holographic", "Inverted", "Machian", "Psionic"
        };

        private static readonly string[] Nouns = new string[]
        {
            "Drive", "Array", "Singularity", "Pulse", "Emitter", "Field", "Engine", "Matrix", "Beacon", "Hull",
            "Transistor", "Module", "System", "Core", "Processor", "Circuit", "Thruster", "Conduit", "Node", "Relic",
            "Spindle", "Lattice", "Carrier", "Generator", "Portal", "Amplifier", "Cradle", "Construct", "Interface", "Scanner",
            "Comet", "Satellite", "Rig", "Gimbal", "Reflector", "Lens", "Prism", "Cabinet", "Transmitter", "Colossus",
            "Overdrive", "Hullplate", "Antimatter", "Exosuit", "Synth", "Outpost", "Junction", "Relayer", "Reactor", "Module",
            "Furnace", "Singulator", "Filter", "Obelisk", "Plates", "Membrane", "Hatch", "Stabilizer", "Deck", "Lab",
            "Chamber", "Vault", "Blade", "Compressor", "Inverter", "Disruptor", "Mirror", "Nodefield", "Spire", "Spike",
            "Fragment", "Emitter", "Drivetrain", "Instrument", "Gyroscope", "Motor", "Container", "Hinge", "Dock", "Crate",
            "Pod", "Hub", "Scope", "Boom", "Cylinder", "Plate", "Shell", "Fuse", "Coil", "Canister",
            "Receptacle", "Manifold", "Crawler", "Loom", "Coupler", "Anchor", "Reflector", "Pylon", "Antenna", "Probe"
        };
    }
}
#endif
