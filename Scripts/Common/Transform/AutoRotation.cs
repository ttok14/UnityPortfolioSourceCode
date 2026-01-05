using UnityEngine;

public class AutoRotation : MonoBehaviour, IUpdatable
{
    [SerializeField]
    private Space _space;
    [SerializeField]
    private float _xEuler;
    [SerializeField]
    private float _yEuler;
    [SerializeField]
    private float _zEuler;

    void IUpdatable.OnUpdate()
    {
        transform.Rotate(_xEuler * Time.deltaTime, _yEuler * Time.deltaTime, _zEuler * Time.deltaTime, _space);
    }

    void OnEnable()
    {
        if (UpdateManager.HasInstance)
            UpdateManager.Instance.RegisterSingle(this);
    }

    void OnDisable()
    {
        if (UpdateManager.HasInstance)
            UpdateManager.Instance.UnregisterSingle(this);
    }
}
