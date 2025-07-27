using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VE2.NonCore.Platform.Internal
{
    public class PlayerBrowserWorldView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _worldNameText;
        [SerializeField] private Button _worldButton;
        [SerializeField] public VerticalLayoutGroup InstancesLayoutGroup;

        public event Action OnWorldButtonClicked;

        public void Setup(string worldName)
        {
            _worldNameText.text = worldName;
            _worldButton.onClick.AddListener(() => OnWorldButtonClicked?.Invoke());
        }
    }
}
