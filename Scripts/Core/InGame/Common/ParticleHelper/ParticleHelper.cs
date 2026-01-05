using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public static class ParticleHelper
{
    public static void Custom_SetLength(
        this ParticleSystem ps,
        Vector3 startPosition,
        Vector3 endPosition,
        float? thickness = null)
    {
        if (!ps)
            return;

        float distance = Vector3.Distance(startPosition, endPosition);
        var main = ps.main;

        if (main.startSize3D)
        {
            if (thickness.HasValue)
            {
                main.startSizeX = GetParticleCurveValue(main.startSizeX, thickness.Value, thickness.Value);
                main.startSizeZ = GetParticleCurveValue(main.startSizeZ, thickness.Value, thickness.Value);
            }

            main.startSizeY = GetParticleCurveValue(main.startSizeY, distance);
        }
    }

    public static MinMaxCurve GetConstantValue(float value)
    {
        return new MinMaxCurve(value);
    }

    public static MinMaxCurve GetRandomValue(float min, float max)
    {
        return new MinMaxCurve(min, max);
    }

    public static MinMaxCurve GetParticleCurveValue(MinMaxCurve srcCurve, float constantOrMin, float optionalMax = 0f)
    {
        switch (srcCurve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return new MinMaxCurve(constantOrMin);
            case ParticleSystemCurveMode.TwoConstants:
                return new MinMaxCurve(constantOrMin, optionalMax > 0 ? optionalMax : constantOrMin);
            default:
                return srcCurve;
        }
    }

    public static bool IsPlaying(this IEnumerable<ParticleSystem> psEnumerable)
    {
        foreach (var p in psEnumerable)
        {
            if (p && p.isPlaying)
            {
                return true;
            }
        }
        return false;
    }

    public static float Custom_GetShortestApproximateLifeTime(ParticleSystem ps)
    {
        if (!ps)
            return 0f;

        var lifeTime = ps.main.startLifetime;
        switch (lifeTime.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return lifeTime.constant;
            case ParticleSystemCurveMode.TwoConstants:
                return lifeTime.constantMin;
            default:
                return lifeTime.constantMin;
        }
    }

    public static float Custom_GetShortestApproximateLifeTime(this IEnumerable<ParticleSystem> psEnumerable)
    {
        float shortest = float.MaxValue;

        foreach (var p in psEnumerable)
        {
            if (!p)
                continue;

            float estimated = Custom_GetShortestApproximateLifeTime(p);

            if (estimated < shortest)
                shortest = estimated;
        }

        return shortest;
    }
}
