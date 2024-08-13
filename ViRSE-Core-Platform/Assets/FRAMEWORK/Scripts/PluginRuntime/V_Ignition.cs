using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViRSE.FrameworkRuntime;

namespace ViRSE.PluginRuntime
{
    //Maybe this should hold the player config and network config (and maybe also platform config) as separate configs, and create each of those elements separately 
    public class V_Ignition : MonoBehaviour
    {
        [SerializeField] private ServerType serverType;
        public ServerType ServerType => serverType;

        private PluginRuntime _pluginRuntime;
        private IFrameworkRuntime _frameworkRuntime;

        private void Start()
        {
            GameObject frameworkGO = GameObject.Find("ViRSE_Runtime");

            if (frameworkGO == null)
            {
                _frameworkRuntime = CreateNewFrameworkRuntime();
                _frameworkRuntime.Initialize(serverType);
            }

            _pluginRuntime = new(_frameworkRuntime, serverType, transform);
        }

        private IFrameworkRuntime CreateNewFrameworkRuntime()
        {
            GameObject runTimePrefab = Resources.Load<GameObject>("ViRSE_Runtime");
            GameObject frameworkGO = GameObject.Instantiate(runTimePrefab);
            DontDestroyOnLoad(frameworkGO);

            return frameworkGO.GetComponent<ViRSERuntime>();
        }

        private void FixedUpdate()
        {
            _pluginRuntime.HandleNetworkUpdate();
        }

        private void OnDestroy()
        {
            _pluginRuntime.TearDown();
        }
    }
}
