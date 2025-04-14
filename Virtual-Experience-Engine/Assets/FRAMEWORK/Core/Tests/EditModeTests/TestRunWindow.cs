using UnityEditor;
using UnityEngine;

public class TestResultsWindow : EditorWindow
{
    private static TestResultsWindow _instance;
    private string _output = "Waiting for test results...";
    private Vector2 _scrollPosition = Vector2.zero;

    public static void ShowWindow()
    {
        _instance = GetWindow<TestResultsWindow>("Test Runner Results");
        _instance.Show();
    }

    public static void UpdateWindow(string results)
    {
        if (_instance == null)
        {
            ShowWindow();
        }

        _instance._output = results;
        _instance.Repaint();
    }

    public static void CloseWindow()
    {
        if (_instance != null)
        {
            _instance.Close();
            _instance = null;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Test Run Results", EditorStyles.boldLabel);
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField(_output, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
        }
    }
}

