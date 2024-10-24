using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class V_InputHandler : MonoBehaviour
{
    private static V_InputHandler _instance;
    public static V_InputHandler Instance {
        get {
            if (_instance == null)
                _instance = FindFirstObjectByType<V_InputHandler>();
            if (_instance == null)
                _instance = new GameObject("V_InputHandler").AddComponent<V_InputHandler>();
            return _instance;
        }
    }

    public InputHandler2D InputHandler2D {get; private set;} = new();

    private void Update()
    {
        InputHandler2D.HandleUpdate();
    }
}

public class InputHandler2D 
{
    public event Action OnMouseLeftClick;

    public void HandleUpdate()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            OnMouseLeftClick?.Invoke();
        }
            
    }
}
