using UnityEngine;

public class FX3DMarker : FXBase
{
    [SerializeField]
    private Transform _heightAdjustor;

    public override bool ActivateLateUpdate => false;

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    //public void SetHeight(float height)
    //{
    //    _heightAdjustor.position = new Vector3(0, height, 0);
    //}
}
