using System;
using System.Collections.Generic;
using UnityEngine;

public class EntityAnimationTriggerBuffer
{
    private readonly Dictionary<int, Dictionary<EntityAnimationStateID, float>> _triggerDataByControllerNameHash;

    public EntityAnimationTriggerBuffer()
    {
        _triggerDataByControllerNameHash = new Dictionary<int, Dictionary<EntityAnimationStateID, float>>();

        foreach (var data in GameDBManager.Instance.Container.AnimationTable_data)
        {
            int key = Animator.StringToHash(data.Value.ControllerName);
            if (_triggerDataByControllerNameHash.TryGetValue(key, out var dic) == false)
            {
                dic = new Dictionary<EntityAnimationStateID, float>();
                _triggerDataByControllerNameHash.Add(key, dic);
            }

            bool parsed = Enum.TryParse(data.Value.StateName, out EntityAnimationStateID stateId);
            if (parsed)
                dic.Add(stateId, data.Value.TriggerAt);
            else
                TEMP_Logger.Err($"Failed to parse Animation State ID | Failed to StateName : {data.Value.StateName} , ControllerName : {data.Value.ControllerName}");
        }
    }

    public Dictionary<EntityAnimationStateID, float> GetData(RuntimeAnimatorController controller)
    {
        if (_triggerDataByControllerNameHash.TryGetValue(Animator.StringToHash(controller.name), out var data) == false)
            return null;

        return data;
    }
}
