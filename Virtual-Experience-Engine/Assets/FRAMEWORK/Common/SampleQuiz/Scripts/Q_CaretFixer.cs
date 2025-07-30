using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Add to input fields where text size is autosize to prevent outsize carets with empty strings
/// </summary>
public class Q_CaretFixer : MonoBehaviour
{
    TMP_InputField inputField;
    Color originalColor;
    void Start()
    {
        inputField = GetComponent<TMP_InputField>();
        originalColor = inputField.caretColor;
    }

    void Update()
    {
        if (inputField.text.Length == 0)
            inputField.caretColor = new Color(1f, 1f, 1f, 0f);
        else
            inputField.caretColor = originalColor;

    }
}
