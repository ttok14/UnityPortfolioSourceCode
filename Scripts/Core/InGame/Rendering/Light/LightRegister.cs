using UnityEngine;

public class LightRegister : MonoBehaviour
{
    [SerializeField]
    LightType _type;

    [SerializeField]
    private Light _light;

    private void Awake()
    {
        if (LightManager.HasInstance)
            LightManager.Instance.RegisterLight(_type, _light);
    }

    private void OnDestroy()
    {
        if (LightManager.HasInstance)
            LightManager.Instance.UnregisterLight(_type);
    }
}
