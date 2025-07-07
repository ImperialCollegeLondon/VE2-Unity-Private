#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace VE2.Common.Shared
{
    public class VE2QuickSetupUtility
    {
        [MenuItem("VE2/VE2 Quick Setup", priority = -998)]
        internal static void ShowWindow()
        {
            var window = ScriptableObject.CreateInstance<VE2QuickSetupWindow>();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 500, 650);
            window.titleContent = new GUIContent("VE2 Quick Setup");
            window.Show();
        }
    }

    internal class VE2QuickSetupWindow : EditorWindow
    {
        private bool asmdef = true;
        private bool tmp = true;
        private bool layersAndTags = true;
        private bool urp = true;
        private bool enableXR = true;
        private bool enableOculusProfile = true;
        private bool editorToolbox = true;
        private bool createScene = true;

        private Vector2 scroll;

        private void OnGUI()
        {
            EditorGUILayout.Space();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawCheckbox(ref asmdef,
                "Create VE2 Assembly Definition",
                "Creates an ASMDEF in Assets/Scripts, preconfigured with required VE2 references. Ensure all your scripts live within this folder.");

            DrawCheckbox(ref tmp,
                "Import TMP Essentials",
                "Imports the TextMeshPro Essentials package, this is required for TMP to function correctly.");

            DrawCheckbox(ref layersAndTags,
                "Configure Layers and Tags",
                "VE2 requires a specific setup for layers and tags.",
                "⚠️ Note: This will override the project's existing layers and tags!");

            DrawCheckbox(ref urp,
                "Setup Universal Render Pipeline (URP)",
                "Configures your project with the same URP settings that were used when creating the VE2 sample scene.");

            DrawCheckbox(ref enableXR,
                "Enable XR Plugin Management (OpenXR)",
                "Enables OpenXR as the active XR plugin in the project.");

            DrawCheckbox(ref enableOculusProfile,
                "Enable Oculus Touch Interaction Profile",
                "Enables the Oculus Touch interaction profile for OpenXR.");

            DrawCheckbox(ref editorToolbox,
                "Configure Editor Toolbox",
                "Creates a preconfigured EditorToolbox settings file to ensure VE2 inspectors behave correctly.");

            DrawCheckbox(ref createScene,
                "Create Quick Start Scene",
                "Creates a new scene in Assets/Scenes, preconfigured with VE2 utilities and some example interactions.");

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(!AnySelected());
            if (GUILayout.Button("Setup VE2", GUILayout.Height(40)))
            {
                RunSetup();
            }
            EditorGUI.EndDisabledGroup();
        }

    private void DrawCheckbox(ref bool toggle, string title, string description, string warning = null)
    {
        toggle = EditorGUILayout.BeginToggleGroup(title, toggle);
        EditorGUILayout.LabelField(description, EditorStyles.wordWrappedLabel);

        if (!string.IsNullOrEmpty(warning))
        {
            var warningStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                normal = { textColor = Color.yellow },
                fontStyle = FontStyle.Bold
            };
            EditorGUILayout.LabelField(warning, warningStyle);
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.EndToggleGroup();
    }

        private bool AnySelected()
        {
            return asmdef || tmp || layersAndTags || urp || enableXR || enableOculusProfile || editorToolbox || createScene;
        }

        private void RunSetup()
        {
            if (asmdef)
                VE2AutoAsmDef.CreateOrUpdateAsmdef();

            if (tmp)
                VE2TMPSetup.ImportTextMeshProEssentials();

            if (layersAndTags)
                VE2LayerAutoConfig.ConfigureLayersAndTags();

            if (urp)
                VE2URPSetup.SetupURP();

            if (enableXR)
                VE2SetupXR.EnableXRPlugInManagement();

            if (enableOculusProfile)
                VE2SetupXR.EnableOpenXRFeatures();

            if (editorToolbox)
                VE2AutoEditorToolboxSetup.CreateToolboxEditorSettingsAsset();

            if (createScene)
                VE2SceneSetupHelper.CreateQuickStartScene();

            if (enableXR)
            {
                EditorUtility.DisplayDialog("VE2 Setup Complete", "Your project has been configured for VE2.\n\nIt is recommended to restart Unity before testing in VR", "Understood");
            }
            else
            {
                EditorUtility.DisplayDialog("VE2 Setup Complete", "Your project has been configured for VE2.", "OK");
            }
        }
    }
}
#endif
