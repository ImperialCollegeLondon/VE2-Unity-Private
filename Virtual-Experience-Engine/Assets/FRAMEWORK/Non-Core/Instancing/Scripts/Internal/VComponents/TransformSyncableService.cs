using UnityEngine;
using VE2.Common.Shared;
using VE2.NonCore.Instancing.API;

namespace VE2.NonCore.Instancing.Internal
{
    public class TransformSyncableService
    {
        #region Interfaces
        public ITransformSyncableStateModule StateModule => _stateModule;
        private ITransformWrapper _transformWrapper;
        private IInstanceServiceInternal _instanceService;
        private bool _isHost => _instanceService.IsHost;
        #endregion

        #region Modules
        private readonly TransformSyncableStateModule _stateModule;
        private readonly TransformSyncableStateConfig _config;
        #endregion

        internal TransformSyncableService(TransformSyncableStateConfig config, string id, IWorldStateSyncableContainer worldStateSyncableContainer,
            IInstanceServiceInternal instanceService, ITransformWrapper transformWrapper)
        {
            _config = config;
            _transformWrapper = transformWrapper;
            _instanceService = instanceService;

            _stateModule = new TransformSyncableStateModule(transformWrapper, config, id, worldStateSyncableContainer);

            _stateModule.OnReceiveState.AddListener(HandleRecieveState);
        }

        public void HandleFixedUpdate()
        {
            _stateModule.HandleFixedUpdate();

            if (_instanceService.IsConnectedToServer && _isHost)
            {
                Debug.Log("Sending transform state from host: ");
                _stateModule.SetStateFromHost(_transformWrapper);
            }
        }

        internal void HandleRecieveState(ITransformWrapper transformWrapper)
        {
            if (_isHost)
            {
                Debug.Log("Received transform state on host, but we are the host, so not updating our own transform.");
                // If we are the host, we don't need to update our own transform
                return;
            }

            // Update the transform with the received state
            Debug.Log("Received transform state from non-host, updating our transform.");
            _transformWrapper.SetLocalPositionAndRotation(transformWrapper.localPosition, transformWrapper.localRotation);
            _transformWrapper.scale = transformWrapper.scale;
        }

        public void TearDown()
        {
            _stateModule.OnReceiveState.RemoveListener(HandleRecieveState);
            _stateModule.TearDown();
            _transformWrapper = null;
            _instanceService = null;
        }
    }
}
