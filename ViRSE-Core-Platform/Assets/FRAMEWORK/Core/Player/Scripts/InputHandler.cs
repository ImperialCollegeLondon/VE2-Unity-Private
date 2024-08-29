using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace ViRSE.FrameworkRuntime.LocalPlayerRig
{
    public class InputHandler : MonoBehaviour
    {
        private static InputHandler _instance = null;
        public static InputHandler Instance { 
            get {
                if (_instance == null)
                    _instance = FindObjectOfType<InputHandler>();
                
                return _instance;
            }
            private set {  
                _instance = value; 
            }
        }

        public UnityEvent OnMouseLeftClick { get; private set; } = new();

        private void Awake()
        {
            Instance = this;
        }

        void Update()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                OnMouseLeftClick?.Invoke();
            }
        }
    }
}
