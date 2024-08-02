using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE
{
    public class AssetLoader : MonoBehaviour
    {
        public static AssetLoader Instance;

        [SerializeField] private GameObject localPlayerRigPrefab;
        public GameObject LocalPlayerRigPrefab => localPlayerRigPrefab;

        private void Awake()
        {
            Instance = this;

        }
    }
}
