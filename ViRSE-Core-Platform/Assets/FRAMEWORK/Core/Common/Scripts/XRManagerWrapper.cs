using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

public interface IXRManagerWrapper
{
    public IEnumerator InitializeLoader() => XRManagerSettings.InitializeLoader();
    public void DeinitializeLoader() => XRManagerSettings.DeinitializeLoader();
    public XRLoader ActiveLoader => XRManagerSettings.activeLoader;
    public bool IsInitializationComplete => XRManagerSettings.isInitializationComplete;
    public void StartSubsystems() => XRManagerSettings.StartSubsystems();
    public void StopSubsystems() => XRManagerSettings.StopSubsystems();
    protected XRManagerSettings XRManagerSettings { get; }
}

public class XRManagerWrapper : IXRManagerWrapper
{
    XRManagerSettings IXRManagerWrapper.XRManagerSettings => XRGeneralSettings.Instance.Manager;
}
