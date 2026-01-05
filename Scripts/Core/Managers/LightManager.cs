using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightManager : SingletonBase<LightManager>
{
    Dictionary<LightType, Light> _lights = new Dictionary<LightType, Light>();

    Color _originalColor;
    Vector3 _originalShadowLightForward;

    Coroutine _transitionCo;

    public Light ShadowLight => _lights[LightType.ShadowLight];
    public Vector3 ShadowLightApproxPosition { get; private set; }
    public Vector3 ShadowLightApproxForward { get; private set; }
    public Vector3 ShadowLightApproxEulerAngles { get; private set; }

    public void RegisterLight(LightType type, Light light)
    {
        _lights[type] = light;

        if (type == LightType.ColorTintLight)
        {
            _originalColor = light.color;
        }
        else if (type == LightType.ShadowLight)
        {
            _originalShadowLightForward = light.transform.forward;
            DoUpdateShadowLightInfo();
        }

        TEMP_Logger.Deb($"Light Registered | Type : {type}, Light : {light}");
    }

    void DoUpdateShadowLightInfo()
    {
        var ts = _lights[LightType.ShadowLight].transform;

        ShadowLightApproxPosition = ts.position;
        ShadowLightApproxForward = ts.forward;
        ShadowLightApproxEulerAngles = ts.eulerAngles;
    }

    public void UnregisterLight(LightType type)
    {
        _lights[type] = null;

        TEMP_Logger.Deb($"Light Unregistered | Type : {type}");
    }

    public void ToDayTime()
    {
        if (_transitionCo != null)
            CoroutineRunner.Instance.Stop(_transitionCo);

        _transitionCo = CoroutineRunner.Instance.RunCoroutine(DoTransitionCo(_originalColor, _originalShadowLightForward));
    }

    public void ToNightTime()
    {
        if (_transitionCo != null)
            CoroutineRunner.Instance.Stop(_transitionCo);

        _transitionCo = CoroutineRunner.Instance.RunCoroutine(DoTransitionCo(new Color(0.95f, 0.1f, 0f), Quaternion.AngleAxis(180f, Vector3.up) * _originalShadowLightForward));
    }

    IEnumerator DoTransitionCo(Color colorTo, Vector3 forwardTo)
    {
        float startedAt = Time.time;
        float duration = 3f;

        var colorFrom = _lights[LightType.ColorTintLight].color;
        var forwardFrom = _lights[LightType.ShadowLight].transform.forward;

        while (startedAt + duration >= Time.time)
        {
            float t = (Time.time - startedAt) / duration;

            var newForward = Vector3.Lerp(forwardFrom, forwardTo, t);

            _lights[LightType.ColorTintLight].color = Color.Lerp(colorFrom, colorTo, t);
            _lights[LightType.ShadowLight].transform.forward = newForward;

            DoUpdateShadowLightInfo();

            yield return null;
        }

        _transitionCo = null;
    }

    public override void Release()
    {
        base.Release();
        _lights.Clear();
    }
}
