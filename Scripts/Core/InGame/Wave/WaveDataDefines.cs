using GameDB;
using System;

public abstract class RuntimeWaveCmdBase
{
    public float ResumeDelay;
    public float Chance;

    public bool IsWaitForClear;

    public RuntimeWaveCmdBase(WaveSequenceTable data)
    {
        ResumeDelay = data.ResumeDelay;
        Chance = data.Chance;
        IsWaitForClear = data.CmdType == E_WaveCommandType.WaitForClear;
    }

    public abstract void Execute();
}

//------ 실 타입 정의 --------//

public class RuntimeSpawnCmd : RuntimeWaveCmdBase
{
    public uint EntityID;
    public int Count;
    public float Interval;
    public SpawnStrategyType Strategy; // 미리 파싱된 Enum

    public RuntimeSpawnCmd(WaveSequenceTable data) : base(data)
    {
        EntityID = data.IntValue01;
        Count = (int)data.IntValue02;
        Interval = data.FloatValue01;

        if (string.IsNullOrEmpty(data.StringValue01) || Enum.TryParse(data.StringValue01, true, out Strategy) == false)
        {
            Strategy = SpawnStrategyType.Distribute; // 기본값
        }
    }

    public override void Execute()
    {
        InGameManager.Instance.EnemyCommander.SpawnController.SpawnEnemyWave(this);
    }
}

public class RuntimeCameraCmd : RuntimeWaveCmdBase
{
    public float Duration;
    public float FOV;

    public RuntimeCameraCmd(WaveSequenceTable data) : base(data)
    {
        Duration = data.FloatValue01;
        FOV = data.FloatValue02;
    }

    public override void Execute()
    {
        if (FOV > 0)
            CameraManager.Instance.InGameController.SetManualStatePositionAndFov(
                InGameManager.Instance.EnemyCommander.SpawnController.GetCenterPosition(), FOV);
        else
            CameraManager.Instance.InGameController.SetManualStatePosition(
                InGameManager.Instance.EnemyCommander.SpawnController.GetCenterPosition());
    }
}

public class RuntimeNotificationCmd : RuntimeWaveCmdBase
{
    public uint MessageLevel;
    public string Message;

    public RuntimeNotificationCmd(WaveSequenceTable data) : base(data)
    {
        MessageLevel = data.IntValue01;
        Message = data.StringValue01;
    }

    public override void Execute()
    {
        if (MessageLevel == 0)
            UIToastSystem.ShowToast(UIToastSystem.ToastType.Center_StaticInformaitve, Message);
        else
            UIToastSystem.ShowToast(UIToastSystem.ToastType.Center_StaticWarning, Message);
    }
}

// 4. Sound Command
public class RuntimeSoundCmd : RuntimeWaveCmdBase
{
    public string AudioKey;

    public RuntimeSoundCmd(WaveSequenceTable data) : base(data)
    {
        AudioKey = data.StringValue01;
    }

    public override void Execute()
    {
        AudioManager.Instance.Play(AudioKey);
    }
}

public class RuntimeFXCmd : RuntimeWaveCmdBase
{
    public string FXKey;

    public RuntimeFXCmd(WaveSequenceTable data) : base(data)
    {
        FXKey = data.StringValue01;
    }

    public override void Execute()
    {
        FXSystem.PlayFX(FXKey, startPosition: InGameManager.Instance.EnemyCommander.SpawnController.GetCenterPosition());
    }
}

public class RuntimeWaitCmd : RuntimeWaveCmdBase
{
    public RuntimeWaitCmd(WaveSequenceTable data) : base(data) { }

    public override void Execute() { }
}
