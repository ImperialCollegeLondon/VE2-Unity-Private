using System;

namespace ViRSE.FrameworkRuntime
{
    public interface IPrimaryServerCommsService
    {
        public void ConnectToServer(ServerType serverType);
        public void SendServerRegistrationRequest(ServerRegistrationRequest populationRegistration);

        public event Action<byte[]> OnReceiveNetcodeConfirmation;
        public event Action<byte[]> OnReceiveServerRegistrationConfirmation;
        public event Action<byte[]> OnReceivePopulationUpdate;
        public event Action OnDisconnectedFromServer;
    }
}
