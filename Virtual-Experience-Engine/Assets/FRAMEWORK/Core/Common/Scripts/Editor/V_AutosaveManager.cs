#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace VE2.Core.Common
{
    [ExecuteInEditMode]
    public class V_AutoSaveManager : MonoBehaviour
    {
        [SerializeField] private float minutesBetweenAutosave = 5f;

        private double timeOfLastAutosave;
        private bool isSubscribed = false;

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
    }
}
#endif
