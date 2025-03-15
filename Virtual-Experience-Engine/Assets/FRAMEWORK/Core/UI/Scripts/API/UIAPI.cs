using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VE2.Core.UI.API
{
    /// <summary>
    /// TODO: Document
    /// </summary>
    public class UIAPI : MonoBehaviour
    {
        private static UIAPI _instance;
        private static UIAPI Instance
        { //Reload-proof InstancingAPI
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<UIAPI>();

                if (_instance == null)
                    _instance = new GameObject($"UIAPI{SceneManager.GetActiveScene().name}").AddComponent<UIAPI>();

                return _instance;
            }
        }

        //TODO: Review what really needs to be internal - maybe we want to allow devs to access this?
        public static IPrimaryUIService PrimaryUIService => UIProvider?.PrimaryUIService;
        public static ISecondaryUIService SecondaryUIService => UIProvider?.SecondaryUIService;

        [SerializeField, HideInInspector] private string _uiProviderGOName;
        private IUIProvider _uiProvider;
        internal static IUIProvider UIProvider
        {
            get
            {
                if (Instance._uiProvider == null && !string.IsNullOrEmpty(Instance._uiProviderGOName))
                    Instance._uiProvider = GameObject.Find(Instance._uiProviderGOName)?.GetComponent<IUIProvider>();

                    if (Instance._uiProvider == null)
                    {
                        Debug.LogError("InstanceService is not available");
                        return null;
                    }  

                    return Instance._uiProvider;

            }
            set //Will need to be called externally
            {
                Instance._uiProvider = value;

                if (value != null)
                    Instance._uiProviderGOName = value.GameObjectName;
            }
        }

        private void Awake()
        {
            _instance = this;
            gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
            //gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
        }
    }

    public interface IPrimaryUIService
    {
        public bool IsShowing { get; }

        public void ShowUI();
        public event Action OnUIShow;

        public void HideUI();
        public event Action OnUIHide;

        public void MoveUIToCanvas(Canvas canvas);

        /// <summary>
        /// Takes the target index, returns the actual index - the two may differ if another panel got that index first 
        /// </summary>
        public void AddNewTab(string tabName, GameObject tab, Sprite icon, int targetIndex);

        public void ShowTab(string tabName);
    }

    public interface ISecondaryUIService
    {
        public void MoveUIToCanvas(Canvas canvas);
    }

    public enum IconType
    {
        Plugin,
        PlayerBrowser, 
        Settings, 
        Help,
    }
}
