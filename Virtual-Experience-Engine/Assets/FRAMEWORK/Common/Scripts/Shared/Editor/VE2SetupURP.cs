#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public static class VE2URPSetup
{
    private const string RENDER_ASSET_PATH = "Packages/com.ic.ve2/FRAMEWORK/Common/Shared/URP/URP-HighFidelity.asset";

    [MenuItem("VE2/Set up URP Settings", priority = -1)]
    public static void SetupURP()
    {
        var urpAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(RENDER_ASSET_PATH);

        if (urpAsset == null)
        {
            Debug.LogError("URP Asset not found at: " + RENDER_ASSET_PATH);
            return;
        }

        // Assign to Graphics settings
        GraphicsSettings.defaultRenderPipeline = urpAsset;
        // Assign to all quality levels
        int qualityLevels = QualitySettings.names.Length;
        for (int i = 0; i < qualityLevels; i++)
        {
            QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
            QualitySettings.renderPipeline = urpAsset;
        }

        Debug.Log("URP settings applied successfully.");
    }
}
#endif
