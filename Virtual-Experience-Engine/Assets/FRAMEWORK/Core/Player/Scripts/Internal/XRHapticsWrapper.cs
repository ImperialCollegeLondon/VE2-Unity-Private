using UnityEngine;
using UnityEngine.XR;
using VE2.Core.VComponents.API;


namespace VE2.Core.Player.Internal
{
    internal interface IXRHapticsWrapper
    {
        public void Vibrate( float amplitude, float duration);
    }

    internal class XRHapticsWrapper : IXRHapticsWrapper
    {
        private bool _isLeftController;

        internal XRHapticsWrapper(bool isLeftController)
        {
            _isLeftController = isLeftController;
        }
        public void Vibrate(float amplitude, float duration)
        {
            InputDevice device = GetDeviceForHand();
            if (device.isValid && device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
                device.SendHapticImpulse(0, amplitude, duration);
        }

        private InputDevice GetDeviceForHand()
        {
            if (_isLeftController)
                return InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            else
                return InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        }
    }
}
