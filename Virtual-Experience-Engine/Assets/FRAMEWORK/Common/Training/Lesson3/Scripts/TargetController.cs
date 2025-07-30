using DG.Tweening;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;

//Lives on the target GameObject
public class TargetController : MonoBehaviour
{
    [SerializeField] private float _maxTargetPos;
    [SerializeField] private float _minTargetPos;

    [SerializeField] private InterfaceReference<IV_HoldActivatable> _buttonRight;
    [SerializeField] private InterfaceReference<IV_HoldActivatable> _buttonLeft;
    [SerializeField] private InterfaceReference<IV_SlidingAdjustable> _speedSlider;
    [SerializeField] private InterfaceReference<IV_NetworkObject> _networkObject;

    private Vector3 _initialScale;

    public PingPongGameColor TargetColour { get; private set; } = PingPongGameColor.Red;
    private Renderer _targetRenderer;

    private void Awake()
    {
        _targetRenderer = GetComponent<Renderer>();
        _targetRenderer.material.color = Color.red;
        _initialScale = transform.localScale;
    }

    private void Update()
    {
        float speed = _speedSlider.Interface.Value;

        if (_buttonRight.Interface.IsActivated)
            transform.position += Vector3.right * speed * Time.deltaTime;
        else if (_buttonLeft.Interface.IsActivated)
            transform.position += Vector3.left * speed * Time.deltaTime;
    }

    public void HandleScaleWheelAdjusted(float newScale)
    {
        Debug.Log($"Scale changed. New: {newScale}, initial: {_initialScale}");
        // Adjust the scale of the target based on the slider value
        transform.localScale = new(_initialScale.x * newScale, _initialScale.y, _initialScale.z * newScale);
    }

    public void HandleChangeColourButtonPressed()
    {
        if (VE2API.InstanceService.IsHost)
        {
            //Increment the target colour
            TargetColour = (PingPongGameColor)(((int)TargetColour + 1) % 3);
            _networkObject.Interface.UpdateData(TargetColour);
        }
    }

    public void HandleSyncDataUpdated(object receivedData)
    {
        //To be safe, we only accept incomming data if we are not the host 
        //This prevents the host's data being overwritten by anyone else
        if (!VE2API.InstanceService.IsHost)
        {
            //Cast our received data object back into 
            //our syncable PingPongGameColor enum
            TargetColour = (PingPongGameColor)receivedData;
        }

        switch (TargetColour)
        {
            case PingPongGameColor.Red:
                _targetRenderer.material.color = Color.red;
                break;
            case PingPongGameColor.Green:
                _targetRenderer.material.color = Color.green;
                break;
            case PingPongGameColor.Blue:
                _targetRenderer.material.color = Color.blue;
                break;
        }
    }
}
