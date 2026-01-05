using GameDB;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[Attribute_GameDBAccessor()]
public class DBAudio
{
    public static Dictionary<uint, AudioTable> AudioData_ByID;
    public static Dictionary<string, AudioTable> AudioData_ByKey;

    public static AudioTable Get(uint id)
    {
        if (AudioData_ByID.TryGetValue(id, out var target))
            return target;
        return null;
    }

    public static AudioTable Get(string resourceKey)
    {
        if (AudioData_ByKey.TryGetValue(resourceKey, out var target))
            return target;
        return null;
    }

    public static bool GetMinMaxDistance(uint id, out float minDistance, out float maxDistance)
    {
        minDistance = 0f;
        maxDistance = 0f;

        var data = Get(id);
        if (data == null)
            return false;

        if (data.Is3D == false)
        {
            TEMP_Logger.Err("Non3D should not use this value, this is for 3d audio");
            return false;
        }

        GetMinMaxDistance(data.DistanceType, out minDistance, out maxDistance);

        return true;
    }

    public static void GetMinMaxDistance(E_Audio3D_DistanceType distanceType, out float minDistance, out float maxDistance)
    {
        switch (distanceType)
        {
            case E_Audio3D_DistanceType.Small:
                minDistance = 1f;
                maxDistance = 17f;
                break;
            case E_Audio3D_DistanceType.Medium:
                minDistance = 2.5f;
                maxDistance = 19f;
                break;
            case E_Audio3D_DistanceType.Large:
                minDistance = 6f;
                maxDistance = 40f;
                break;
            default:
                minDistance = 0;
                maxDistance = 0;
                break;
        }
    }

    public static bool Is3DAudioInRange(AudioTable data, Vector3 listenerPosition, Vector3 speakerPosition)
    {
        if (data == null)
            return false;
        if (data.Is3D == false)
            return true;

        var sqrDist = Vector3.SqrMagnitude(listenerPosition - speakerPosition);

        GetMinMaxDistance(data.DistanceType, out float minDist, out float maxDist);

        float sqrAudioableRange = maxDist * maxDist;

        return sqrDist <= sqrAudioableRange;
    }

    public static void InitializeBootstrap()
    {
        var bin = Resources.Load<TextAsset>("Table/AudioTable");
        GameDBManager.Instance.Container.AudioTable_data = GameDBHelper.LoadTableBinary(nameof(GameDBContainer.AudioTable_data), bin.bytes) as Dictionary<uint, AudioTable>;
        AudioData_ByID = GameDBManager.Instance.Container.AudioTable_data;
        AudioData_ByKey = GameDBManager.Instance.Container.AudioTable_data.ToDictionary(t => t.Value.ResourceKey, t => t.Value);
    }

    public static void OnTableReady()
    {
        AudioData_ByID = GameDBManager.Instance.Container.AudioTable_data;
        AudioData_ByKey = GameDBManager.Instance.Container.AudioTable_data.ToDictionary(t => t.Value.ResourceKey, t => t.Value);
    }


    public static void Release()
    {
        AudioData_ByID = null;
    }
}
