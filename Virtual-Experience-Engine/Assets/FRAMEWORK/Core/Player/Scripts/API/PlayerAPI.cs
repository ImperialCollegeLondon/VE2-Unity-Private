using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VE2.Core.VComponents.API;

namespace VE2.Core.Player.API
{
    public class PlayerAPI : MonoBehaviour 
    {
        private static PlayerAPI _instance;
        private static PlayerAPI Instance
        { //Reload-proof singleton
            get
            {
                //if we've moved to a different scene, this will be null, so we can find the locator for the new scene
                if (_instance == null)
                    _instance = FindFirstObjectByType<PlayerAPI>();

                if (_instance == null && !Application.isPlaying)
                    _instance = new GameObject($"PlayerAPI{SceneManager.GetActiveScene().name}").AddComponent<PlayerAPI>();

                return _instance;
            }
        }

        public static IPlayerService Player => PlayerServiceProvider.PlayerService;

        [SerializeField] private string PlayerServiceProviderGOName;
        private IPlayerServiceProvider _playerServiceProvder;
        internal static IPlayerServiceProvider PlayerServiceProvider
        {
            private get
            {
                if (Instance._playerServiceProvder == null && !string.IsNullOrEmpty(Instance.PlayerServiceProviderGOName))
                    Instance._playerServiceProvder = GameObject.Find(Instance.PlayerServiceProviderGOName)?.GetComponent<IPlayerServiceProvider>();

                return Instance._playerServiceProvder;
            }
            set //Will need to be called externally
            {
                Instance._playerServiceProvder = value;

                if (value != null)
                    Instance.PlayerServiceProviderGOName = value.GameObjectName;
            }
        }

        //Lives here so we can inject a stub for testing
        private IInputHandler _inputHandler;
        internal static IInputHandler InputHandler //Returns the default InputHandler 
        {
            get
            {
                if (!Application.isPlaying)
                {
                    Debug.LogError("InputHandler is only available at runtime");
                    return null;
                }
                    
                Instance._inputHandler ??= FindFirstObjectByType<InputHandler>();
                Instance._inputHandler ??= new GameObject("V_InputHandler").AddComponent<InputHandler>();
                return Instance._inputHandler;
            }
        }

        public static bool HasMultiPlayerSupport => LocalClientIDProvider != null && LocalClientIDProvider.IsEnabled;
        [SerializeField, HideInInspector] private string _playerSyncProviderGOName;
        private ILocalClientIDProvider _playerSyncProvider;
        internal static ILocalClientIDProvider LocalClientIDProvider {
            get 
            {
                if (Instance._playerSyncProvider == null && !string.IsNullOrEmpty(Instance._playerSyncProviderGOName))
                    Instance._playerSyncProvider = GameObject.Find(Instance._playerSyncProviderGOName)?.GetComponent<ILocalClientIDProvider>();

                if (Instance._playerSyncProvider != null && Instance._playerSyncProvider.IsEnabled)
                    return Instance._playerSyncProvider;
                else 
                    return null;
            }
            set
            {
                Instance._playerSyncProvider = value;

                if (value != null)
                    Instance._playerSyncProviderGOName = value.GameObjectName;
            }
        }

        private void Awake()
        {
            if (FindObjectsByType<PlayerAPI>(FindObjectsSortMode.None).Length > 1)
            {
                Debug.LogError("There should only be one PlayerAPI in the scene");
                Destroy(gameObject);
            }

            _instance = this;
            //gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
            gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
        }
    }
}