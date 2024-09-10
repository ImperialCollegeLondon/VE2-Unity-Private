using System;

namespace ViRSE.FrameworkRuntime
{
    public interface IPrimaryServerCommsService
    {
        public bool IsReadyToTransmit { get; }

        public void SendServerRegistrationRequest(byte[] serverRegistrationByts);

        public event Action<byte[]> OnReceiveNetcodeConfirmation;
        public event Action<byte[]> OnReceiveServerRegistrationConfirmation;
        public event Action<byte[]> OnReceivePopulationUpdate;
        public event Action OnDisconnectedFromServer;
    }
}
