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

        public void InitializeLoader() => StartCoroutine(nameof(InitializeLoaderAsync));
        
        private IEnumerator InitializeLoaderAsync() 
        {
            Debug.Log("Initializing XR Loader");
            yield return XRManagerSettings.InitializeLoader();
            OnLoaderInitialized?.Invoke();
            Debug.Log("XR Loader init complete");
        }

        public void DeinitializeLoader() => XRManagerSettings.DeinitializeLoader();
        public XRLoader ActiveLoader => XRManagerSettings.activeLoader;
        public bool IsInitializationComplete => XRManagerSettings.isInitializationComplete;
        public void StartSubsystems() => XRManagerSettings.StartSubsystems();
        public void StopSubsystems() => XRManagerSettings.StopSubsystems();

        private void Awake()
        {
            //gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
            gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
        }
    }
}
