using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VE2.NonCore.Platform.Internal
{
    //TODO rename PlayerBrowserWorldInfoView?
    public class PlayerBrowserWorldView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _worldNameText;
        [SerializeField] private Button _worldButton;
        [SerializeField] private VerticalLayoutGroup _instancesLayoutGroup;

        public event Action OnWorldButtonClicked;

        private void Awake()
        {
            _worldButton.onClick.AddListener(() => OnWorldButtonClicked?.Invoke());
        }

        public void SetName(string worldName)
        {
            _worldNameText.text = worldName;
        }

        //public PlayerBrowserWorldView

        // public void UpdateWorlds(List<PlayerBrowserWorldInfo> worldInfos)
        // {
        //     // Clear existing world views
        //     foreach (Transform child in _instancesLayoutGroup.transform)
        //     {
        //         Destroy(child.gameObject);
        //     }

        //     // Create new world views
        //     foreach (var worldInfo in worldInfos)
        //     {
        //         GameObject worldView = Instantiate(Resources.Load<GameObject>("PlayerBrowserWorldViewPrefab"), _instancesLayoutGroup.transform);
        //         worldView.GetComponent<PlayerBrowserWorldView>().Initialize(worldInfo);
        //     }
        // }
    }
}
