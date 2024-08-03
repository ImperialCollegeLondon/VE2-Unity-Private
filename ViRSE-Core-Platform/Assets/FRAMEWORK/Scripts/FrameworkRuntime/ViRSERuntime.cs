using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using ViRSE.FrameworkRuntime.LocalPlayerRig;

namespace ViRSE.FrameworkRuntime
{
    public interface IFrameworkRuntime
    {
        public IPrimaryServerService PrimaryServerService { get; }
        public ILocalPlayerRig LocalPlayerRig { get; }

        public event Action OnFrameworkReady;

        public void Initialize(ServerType serverType);
    }

    public class ViRSERuntime : MonoBehaviour, IFrameworkRuntime
    {
        [SerializeField] private GameObject primaryServerServicePrefab;
        [SerializeField] private GameObject localPlayerRigPrefab;

        public event Action OnFrameworkReady;
        public IPrimaryServerService PrimaryServerService { get; private set; }
        public ILocalPlayerRig LocalPlayerRig => _localPlayerService;

        private LocalPlayerService _localPlayerService;
        private ServerType _serverType;

        public void Initialize(ServerType serverType)
        {
            _serverType = serverType;
            DontDestroyOnLoad(gameObject);

            if (_serverType != ServerType.Offline)
            {
                Debug.Log("Make server");
                PrimaryServerService primaryServerService = Instantiate(primaryServerServicePrefab).GetComponent<PrimaryServerService>();
                DontDestroyOnLoad(primaryServerService);
                //primaryServerService. //Needs to be fed the networking type
            }
            else
            {
                Debug.Log("Make player");
                SpawnPlayer();
                OnFrameworkReady?.Invoke();
            }
        }

        private void SpawnPlayer() //TODO
        {
            GameObject localPlayerRigGO = Instantiate(localPlayerRigPrefab, transform);
            _localPlayerService = localPlayerRigGO.GetComponent<LocalPlayerService>();
            //localPlayerRig.Init

            //DontDestroyOnLoad(localPlayerRigGO);
        }
    }
}
