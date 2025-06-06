using UnityEngine;
using DG.Tweening;

public class HubMasterView : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private GameObject _loadingScreen;
    [SerializeField] private GameObject _mainScreen;

    [Header("Panel Pivots")]
    [SerializeField] private Transform _rightPanelPivot;
    [SerializeField] private Transform _leftPanelPivot;
    [SerializeField] private Transform _bottomPanelPivot;

    [Header("Panel Start Transforms")]
    [SerializeField] private Transform _rightPanelStartTransform;
    [SerializeField] private Transform _leftPanelStartTransform;
    [SerializeField] private Transform _bottomPanelStartTransform;

    // Final target transforms (from scene at runtime)
    private Vector3 _rightPanelEndPosition;
    private Vector3 _leftPanelEndPosition;
    private Vector3 _bottomPanelEndPosition;

    private Quaternion _rightPanelEndRotation;
    private Quaternion _leftPanelEndRotation;
    private Quaternion _bottomPanelEndRotation;

    private void Awake()
    {
        // Cache the final "in scene" positions and rotations
        _rightPanelEndPosition = _rightPanelPivot.position;
        _rightPanelEndRotation = _rightPanelPivot.rotation;

        _leftPanelEndPosition = _leftPanelPivot.position;
        _leftPanelEndRotation = _leftPanelPivot.rotation;

        _bottomPanelEndPosition = _bottomPanelPivot.position;
        _bottomPanelEndRotation = _bottomPanelPivot.rotation;

        // Snap them to their defined starting positions/rotations
        _rightPanelPivot.position = _rightPanelStartTransform.position;
        _rightPanelPivot.rotation = _rightPanelStartTransform.rotation;

        _leftPanelPivot.position = _leftPanelStartTransform.position;
        _leftPanelPivot.rotation = _leftPanelStartTransform.rotation;

        _bottomPanelPivot.position = _bottomPanelStartTransform.position;
        _bottomPanelPivot.rotation = _bottomPanelStartTransform.rotation;

        _rightPanelPivot.gameObject.SetActive(false);
        _leftPanelPivot.gameObject.SetActive(false);
        _bottomPanelPivot.gameObject.SetActive(false);
    }

    [SerializeField] private Ease _panelTranslateEase = Ease.InOutQuart;
    [SerializeField] private Ease _panelRotateEase = Ease.InOutSine;
    [SerializeField] private float _panelMoveDuration = 1.15f;
    [SerializeField] private float _panelRotateDuration = 0.7f;

    public void HandleLoadingComplete()
    {
        _rightPanelPivot.gameObject.SetActive(true);
        _leftPanelPivot.gameObject.SetActive(true);
        _bottomPanelPivot.gameObject.SetActive(true);

        _loadingScreen.SetActive(false);
        _mainScreen.SetActive(true);

        // Right panel sequence
        Sequence rightSequence = DOTween.Sequence();
        rightSequence.Append(_rightPanelPivot.DOMove(_rightPanelEndPosition, _panelMoveDuration).SetEase(_panelTranslateEase));
        rightSequence.Append(_rightPanelPivot.DORotateQuaternion(_rightPanelEndRotation, _panelRotateDuration).SetEase(Ease.InOutSine));

        // Left panel sequence
        Sequence leftSequence = DOTween.Sequence();
        leftSequence.Append(_leftPanelPivot.DOMove(_leftPanelEndPosition, _panelMoveDuration).SetEase(_panelTranslateEase));
        leftSequence.Append(_leftPanelPivot.DORotateQuaternion(_leftPanelEndRotation, _panelRotateDuration).SetEase(Ease.InOutSine));

        // Bottom panel sequence
        Sequence bottomSequence = DOTween.Sequence();
        bottomSequence.Append(_bottomPanelPivot.DOMove(_bottomPanelEndPosition, _panelMoveDuration).SetEase(_panelTranslateEase));
        bottomSequence.Append(_bottomPanelPivot.DORotateQuaternion(_bottomPanelEndRotation, _panelRotateDuration).SetEase(Ease.InOutSine));

        // Combine all sequences into a master sequence that plays them in parallel
        DOTween.Sequence()
            .Append(rightSequence)
            .Join(leftSequence)
            .Join(bottomSequence);
    }
}
