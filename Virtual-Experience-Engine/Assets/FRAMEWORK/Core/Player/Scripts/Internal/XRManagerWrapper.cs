using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

namespace VE2.Core.Player.Internal
{
    internal interface IXRManagerWrapper
    {
        public void InitializeLoader();
        public event Action OnLoaderInitialized;
        public void DeinitializeLoader();
        public XRLoader ActiveLoader { get; }
        public bool IsInitializationComplete { get; }
        public void StartSubsystems();
        public void StopSubsystems();
        //protected XRManagerSettings XRManagerSettings { get; }
    }

    internal class XRManagerWrapper : MonoBehaviour, IXRManagerWrapper
    {
        XRManagerSettings XRManagerSettings => XRGeneralSettings.Instance.Manager;

        public event Action OnLoaderInitialized;

        public void InitializeLoader()
        {
            try
            {
                StartCoroutine(nameof(InitializeLoaderAsync));
            }
            catch (Exception e)
            {
                if (Application.isEditor)
                    Debug.LogError($"Failed to initialize XR Loader: {e.Message}");
            }

        }
        private IEnumerator InitializeLoaderAsync()
        {
            Debug.Log("Initializing XR Loader");
            yield return XRManagerSettings.InitializeLoader();
            OnLoaderInitialized?.Invoke();
            Debug.Log("XR Loader init complete");
        }

        public void DeinitializeLoader()
        {
            try
            {
                XRManagerSettings.DeinitializeLoader();
                Debug.Log("XR Loader deinitialized");
            }
            catch (Exception e)
            {
                if (Application.isEditor)
                    Debug.LogError($"Failed to deinitialize XR Loader: {e.Message}");
            }
        }
        public XRLoader ActiveLoader => XRManagerSettings.activeLoader;
        public bool IsInitializationComplete => XRManagerSettings.isInitializationComplete;
        public void StartSubsystems()
        {
            try
            {
                XRManagerSettings.StartSubsystems();
            }
            catch (Exception e)
            {
                if (Application.isEditor)
                    Debug.LogError($"Failed to start XR subsystems: {e.Message}");
                return;
            }
        }
        public void StopSubsystems()
        {
            try
            {
                XRManagerSettings.StopSubsystems();
            }
            catch (Exception e)
            {
                if (Application.isEditor)
                    Debug.LogError($"Failed to stop XR subsystems: {e.Message}");
                return;
            }
        }

        private void Awake()
        {
            //gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
            gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
        }
    }
}
