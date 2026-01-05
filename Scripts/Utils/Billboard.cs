using UnityEngine;

public class Billboard : MonoBehaviour
{
    Camera _mainCam;

    private void OnEnable()
    {
        ApplyBillboard();
    }

    private void Update()
    {
        ApplyBillboard();
    }

    void ApplyBillboard()
    {
        if (_mainCam == null)
            _mainCam = CameraManager.Instance.MainCam;
        transform.rotation = _mainCam.transform.rotation;
    }
}
