using UnityEngine;
using UnityEngine.XR;
using VE2.Core.VComponents.API;


namespace VE2.Core.Player.Internal
{
    internal interface IXRHapticsWrapper
    {
        public void Vibrate(InteractorID interactorID, float amplitude, float duration);
    }

    internal class XRHapticsWrapper : MonoBehaviour, IXRHapticsWrapper
    {
        public void Vibrate(InteractorID interactorID, float amplitude, float duration)
        {
            InputDevice device = GetDeviceForInteractor(interactorID);
            if (device.isValid && device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0, amplitude, duration);
            }
        }

        private InputDevice GetDeviceForInteractor(InteractorID interactorID)
        {
            return interactorID.InteractorType switch
            {
                InteractorType.LeftHandVR => InputDevices.GetDeviceAtXRNode(XRNode.LeftHand),
                InteractorType.RightHandVR => InputDevices.GetDeviceAtXRNode(XRNode.RightHand),
                _ => default
            };
        }

        private void Awake()
        {
            //gameObject.hideFlags = HideFlags.HideInHierarchy; //To hide
            gameObject.hideFlags &= ~HideFlags.HideInHierarchy; //To show
        }
    }

}
