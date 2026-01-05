using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityAnimationParameter
{
    private class ParamInfo
    {
        public readonly int Id;
        public readonly AnimatorControllerParameterType ParameterType;

        public readonly bool Default_BoolValue;
        public readonly int Default_IntValue;
        public readonly float Default_FloatValue;

        public ParamInfo(EntityAnimationParameterType parameter, bool defaultValue)
        {
            Id = Animator.StringToHash(parameter.ToString());
            ParameterType = AnimatorControllerParameterType.Bool;
            Default_BoolValue = defaultValue;
        }

        public ParamInfo(EntityAnimationParameterType parameter, int defaultValue)
        {
            Id = Animator.StringToHash(parameter.ToString());
            ParameterType = AnimatorControllerParameterType.Int;
            Default_IntValue = defaultValue;
        }

        public ParamInfo(EntityAnimationParameterType parameter, float defaultValue)
        {
            Id = Animator.StringToHash(parameter.ToString());
            ParameterType = AnimatorControllerParameterType.Float;
            Default_FloatValue = defaultValue;
        }

        public ParamInfo(EntityAnimationParameterType parameter)
        {
            Id = Animator.StringToHash(parameter.ToString());
            ParameterType = AnimatorControllerParameterType.Trigger;
        }
    }

    private readonly static Dictionary<EntityAnimationParameterType, ParamInfo> Parameters;

    static EntityAnimationParameter()
    {
        Parameters = new Dictionary<EntityAnimationParameterType, ParamInfo>
        {
            [EntityAnimationParameterType.MoveSpeedRate] = new ParamInfo(EntityAnimationParameterType.MoveSpeedRate, 0f),
            [EntityAnimationParameterType.AttackSpeedRate] = new ParamInfo(EntityAnimationParameterType.AttackSpeedRate, 1f),

            [EntityAnimationParameterType.Skill01] = new ParamInfo(EntityAnimationParameterType.Skill01),
            [EntityAnimationParameterType.Skill02] = new ParamInfo(EntityAnimationParameterType.Skill02),
            [EntityAnimationParameterType.Skill03] = new ParamInfo(EntityAnimationParameterType.Skill03),

            // [EntityAnimationParameterType.SkillTarget_Direction] = new ParamInfo(EntityAnimationParameterType.SkillTarget_Direction, 0f),

            [EntityAnimationParameterType.Lumbering] = new ParamInfo(EntityAnimationParameterType.Lumbering),

            [EntityAnimationParameterType.RidingPet] = new ParamInfo(EntityAnimationParameterType.RidingPet, false),

            [EntityAnimationParameterType.Die] = new ParamInfo(EntityAnimationParameterType.Die),
        };
    }

    public static int GetParameterID(EntityAnimationParameterType type)
    {
        if (Parameters.TryGetValue(type, out var info))
            return info.Id;
        return -1;
    }

    public static void SetDefaultParameter(EntityAnimationPart part, Animator anim)
    {
        // 부하가 있으려나?
        foreach (var param in Parameters)
        {
            if (part.HasAnimationParameter(param.Value.Id) == false)
                continue;

            if (param.Value.ParameterType == AnimatorControllerParameterType.Trigger)
            {
                anim.ResetTrigger(param.Value.Id);
                continue;
            }

            SetParameter(part, anim, param.Key, param.Value);
        }
    }

    static void SetParameter(EntityAnimationPart part, Animator anim, EntityAnimationParameterType type, ParamInfo param)
    {
        if (part.HasAnimationParameter(param.Id) == false)
            return;

        switch (param.ParameterType)
        {
            case AnimatorControllerParameterType.Float:
                SetParameter(part, anim, type, param.Default_FloatValue);
                break;
            case AnimatorControllerParameterType.Int:
                SetParameter(part, anim, type, param.Default_IntValue);
                break;
            case AnimatorControllerParameterType.Bool:
                SetParameter(part, anim, type, param.Default_BoolValue);
                break;
            case AnimatorControllerParameterType.Trigger:
                SetParameter(part, anim, type);
                break;
        }
    }

    public static void SetParameter(EntityAnimationPart part, Animator anim, EntityAnimationParameterType type, bool value)
    {
        var param = Parameters[type];
        if (CheckType(part, param, AnimatorControllerParameterType.Bool) == false)
        {
            TEMP_Logger.Err($"Param Type Error : {type}");
            return;
        }

        anim.SetBool(param.Id, value);
    }

    public static void SetParameter(EntityAnimationPart part, Animator anim, EntityAnimationParameterType type, int value)
    {
        var param = Parameters[type];
        if (CheckType(part, param, AnimatorControllerParameterType.Int) == false)
        {
            TEMP_Logger.Err($"Param Type Error : {type}");
            return;
        }

        anim.SetInteger(param.Id, value);
    }

    public static void SetParameter(EntityAnimationPart part, Animator anim, EntityAnimationParameterType type, float value)
    {
        var param = Parameters[type];
        if (CheckType(part, param, AnimatorControllerParameterType.Float) == false)
        {
            TEMP_Logger.Err($"Param Type Error : {type}");
            return;
        }

        anim.SetFloat(param.Id, value);
    }

    public static void SetParameter(EntityAnimationPart part, Animator anim, EntityAnimationParameterType type)
    {
        var param = Parameters[type];
        if (CheckType(part, param, AnimatorControllerParameterType.Trigger) == false)
        {
            TEMP_Logger.Err($"Param Type Error : {type}");
            return;
        }

        anim.SetTrigger(param.Id);
    }

    private static bool CheckType(EntityAnimationPart part, ParamInfo param, AnimatorControllerParameterType type)
    {
        if (param == null)
            return false;

        // 실제로 해당 애니메이터 파라미터와 체크 
        if (part != null && part.AnimatorParamTable.TryGetValue(param.Id, out var p) == false)
        {
            TEMP_Logger.Err($"Given AnimationType does not exist in the real animator animation Paramm list | Type : {param.ParameterType} , {type}");
            return false;
        }

        return param.ParameterType == type;
    }
}
