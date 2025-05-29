using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

internal class HubWorldButtonView : MonoBehaviour
{
    [SerializeField] private Button _worldButton;
    [SerializeField] private Image _worldImage;
    [SerializeField] private TMP_Text _worldNameText;
    [SerializeField] private Image _needsDownloadIcon;
    [SerializeField] private Image _readyIcon;

    public event Action<HubWorldDetails> OnWorldClicked;

    private HubWorldDetails _worldDetails;

    public void SetupView(HubWorldDetails worldDetails)
    {
        _worldDetails = worldDetails;

        _worldNameText.text = worldDetails.Name;

        //TODO
        _needsDownloadIcon.gameObject.SetActive(false);
        _readyIcon.gameObject.SetActive(false);
        //Also need to do image!

        _worldButton.onClick.AddListener(() => OnWorldClicked?.Invoke(_worldDetails));
    }
}
