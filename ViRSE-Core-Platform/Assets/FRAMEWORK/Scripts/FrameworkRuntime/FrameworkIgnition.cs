using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViRSE;
using ViRSE.FrameworkRuntime;

namespace ViRSE
{

    public class FrameworkIgnition : MonoBehaviour
    {
        [SerializeField] private ServerType _serverType;

        private void Awake()
        {
            GameObject frameworkGO = GameObject.Find("ViRSE_Runtime");
            IFrameworkRuntime frameworkRuntime;

            if (frameworkGO != null)
                frameworkRuntime = frameworkGO.GetComponent<IFrameworkRuntime>();
            else
                frameworkRuntime = CreateNewFrameworkRuntime();

            frameworkRuntime.Initialize(_serverType);
        }

        private IFrameworkRuntime CreateNewFrameworkRuntime()
        {
            GameObject runTimePrefab = Resources.Load<GameObject>("ViRSE_Runtime");
            GameObject runTimeInstance = Instantiate(runTimePrefab);
            DontDestroyOnLoad(runTimeInstance);

            IFrameworkRuntime frameworkService = runTimeInstance.GetComponent<IFrameworkRuntime>();
            return frameworkService;
        }
    }
}
