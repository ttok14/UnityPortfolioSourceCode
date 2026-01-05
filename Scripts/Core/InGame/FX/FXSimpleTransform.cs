using UnityEngine;

public class FXSimpleTransform : FXBase
{
    [SerializeField]
    private Space _space;
    [SerializeField]
    private float _xEuler;
    [SerializeField]
    private float _yEuler;
    [SerializeField]
    private float _zEuler;

    public override bool ActivateLateUpdate => true;

    protected override void OnUpdated()
    {
        base.OnUpdated();

        transform.Rotate(_xEuler * Time.deltaTime, _yEuler * Time.deltaTime, _zEuler * Time.deltaTime, _space);
    }
}
