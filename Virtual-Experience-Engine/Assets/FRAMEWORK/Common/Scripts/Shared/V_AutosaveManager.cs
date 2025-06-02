using UnityEngine;

#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace VE2.Common.Shared
{
    [ExecuteInEditMode]
    internal class V_AutoSaveManager : MonoBehaviour
    {
        [SerializeField] private float minutesBetweenAutosave = 5f;

        private double timeOfLastAutosave;
        private bool isSubscribed = false;

        #if UNITY_EDITOR
        private void OnEnable()
        {
            if (Application.isPlaying || isSubscribed)
                return;

            EditorApplication.update += CheckForAutoSave;
            isSubscribed = true;
            timeOfLastAutosave = EditorApplication.timeSinceStartup;
        }

        private void OnDisable()
        {
            if (Application.isPlaying || !isSubscribed)
                return;

            EditorApplication.update -= CheckForAutoSave;
            isSubscribed = false;
        }

        private void CheckForAutoSave()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || !EditorApplication.isFocused)
                return;

            if (EditorApplication.timeSinceStartup - timeOfLastAutosave > (minutesBetweenAutosave * 60))
            {
                if (SceneManager.GetActiveScene().isDirty)
                {
                    Debug.Log($"[VE2 AutoSave] Saving scene: {SceneManager.GetActiveScene().name}");
                    timeOfLastAutosave = EditorApplication.timeSinceStartup;
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                }
            }
        }

        #endif
    }
}
