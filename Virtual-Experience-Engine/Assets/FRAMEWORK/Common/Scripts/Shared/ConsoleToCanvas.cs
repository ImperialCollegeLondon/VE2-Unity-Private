using UnityEngine;
using UnityEngine.UI;
using System.Text;
using TMPro;

public class ConsoleToCanvas : MonoBehaviour
{
    [SerializeField] private TMP_Text logText;

    private StringBuilder logBuilder = new StringBuilder();

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
        logText.text = string.Empty;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string coloredMessage;

        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                // Wrap error/exception/assert messages in red color tag
                coloredMessage = $"<color=red>{logString}</color>";
                break;
            case LogType.Warning:
                // Optional: make warnings yellow for better visibility
                coloredMessage = $"<color=yellow>{logString}</color>";
                break;
            default:
                // Normal log messages stay default color
                coloredMessage = logString;
                break;
        }

        // Append with some separation to keep logs clear
        logBuilder.AppendLine("--- " + coloredMessage);

        if (logText != null)
        {
            logText.text = logBuilder.ToString();
        }
    }
}
