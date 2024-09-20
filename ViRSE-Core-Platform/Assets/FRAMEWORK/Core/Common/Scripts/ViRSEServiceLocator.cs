using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using ViRSE.Core.Shared;

[ExecuteInEditMode]
public class ViRSEServiceLocator : MonoBehaviour
{
    private static ViRSEServiceLocator _instance;
    public static ViRSEServiceLocator Instance {
    get {
            if (_instance == null)
                _instance = FindFirstObjectByType<ViRSEServiceLocator>();

            if (_instance == null)
                _instance = new GameObject("ViRSEServiceLocator").AddComponent<ViRSEServiceLocator>();

            return _instance;
        }
    }

    //Record the gameobject name so we can re-locate multiplayer support after a domain reload
    [SerializeField, HideInInspector] private string _multiplayerSupportGOName;
    private IMultiplayerSupport _multiPlayerSupport;
    public IMultiplayerSupport MultiplayerSupport 
    {
        get 
        {
            if (_multiPlayerSupport == null && !string.IsNullOrEmpty(_multiplayerSupportGOName))
                _multiPlayerSupport = GameObject.Find(_multiplayerSupportGOName)?.GetComponent<IMultiplayerSupport>();

            return _multiPlayerSupport;
        }
        set 
        {
            _multiPlayerSupport = value;

            if (value != null)
                _multiplayerSupportGOName = value.MultiplayerSupportGameObjectName;
        }
    }


    private void Awake()
    {
        _instance = this;
    }
}
