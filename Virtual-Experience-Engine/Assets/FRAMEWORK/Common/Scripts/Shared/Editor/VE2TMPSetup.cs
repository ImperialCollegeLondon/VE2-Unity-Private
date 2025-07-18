#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

internal static class VE2TMPSetup
{
    //[MenuItem("Tools/Setup/Import TMP Essentials")]
    internal static void ImportTextMeshProEssentials()
    {
        var importerType = typeof(TMPro.TMP_PackageResourceImporter);

        var importMethod = importerType.GetMethod(
            "ImportResources",
            BindingFlags.Static | BindingFlags.Public
        );

        if (importMethod == null)
        {
            Debug.LogError("❌ ImportResources method not found on TMP_PackageResourceImporter.");
            return;
        }

        // Call ImportResources(importEssentials: true, importExamples: false, interactive: false)
        object[] parameters = new object[] { true, false, false };
        importMethod.Invoke(null, parameters);

        EditorUtility.RequestScriptReload();
        Debug.Log("✅ TMP Essentials imported programmatically.");
    }

}
#endif


        //EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
