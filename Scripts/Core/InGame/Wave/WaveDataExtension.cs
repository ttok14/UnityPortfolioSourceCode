//using GameDB;

//public static class WaveDataExtension
//{
//    public static WaveSpawnCmdData ToSpawnData(this WaveSequenceTable cmd)
//    {
//        if (cmd.CmdType != E_WaveCommandType.Spawn)
//        {
//            TEMP_Logger.Err($"Invalid Type : {cmd.CmdType} , Only {E_WaveCommandType.Spawn}");
//        }

//        return new WaveSpawnCmdData(
//            spawnPointID: cmd.IntValue01,
//            spawnEntityID: cmd.IntValue02,
//            count: cmd.IntValue03,
//            interval: cmd.FloatValue01,
//            delayAfter: cmd.FloatValue02);
//    }

//    public static WaveWaitCmdData ToWaitData(this WaveSequenceTable cmd)
//    {
//        if (cmd.CmdType != E_WaveCommandType.Wait)
//        {
//            TEMP_Logger.Err($"Invalid Type : {cmd.CmdType} , Only {E_WaveCommandType.Wait}");
//        }

//        return new WaveWaitCmdData(delayAfter: cmd.FloatValue01);
//    }

//    public static WaveCameraCmdData ToCameraData(this WaveSequenceTable cmd)
//    {
//        if (cmd.CmdType != E_WaveCommandType.Camera)
//        {
//            TEMP_Logger.Err($"Invalid Type : {cmd.CmdType} , Only {E_WaveCommandType.Camera}");
//        }

//        return new WaveCameraCmdData(
//            targetID: cmd.IntValue01,
//            fov: cmd.IntValue02,
//            duration: cmd.FloatValue01,
//            delayAfter: cmd.FloatValue02);
//    }

//    public static WaveNotificationCmdData ToNotificationData(this WaveSequenceTable cmd)
//    {
//        if (cmd.CmdType != E_WaveCommandType.Notification)
//        {
//            TEMP_Logger.Err($"Invalid Type : {cmd.CmdType} , Only {E_WaveCommandType.Notification}");
//        }

//        return new WaveNotificationCmdData(
//            messageLevel: cmd.IntValue01,
//            message: cmd.StringValue01,
//            delayAfter: cmd.FloatValue01);
//    }

//    public static WaveSoundCmdData ToSoundData(this WaveSequenceTable cmd)
//    {
//        if (cmd.CmdType != E_WaveCommandType.Sound)
//        {
//            TEMP_Logger.Err($"Invalid Type : {cmd.CmdType} , Only {E_WaveCommandType.Sound}");
//        }

//        return new WaveSoundCmdData(
//            audioKey: cmd.StringValue01,
//            delayAfter: cmd.FloatValue01);
//    }

//    public static WaveFXCmdData ToFXData(this WaveSequenceTable cmd)
//    {
//        if (cmd.CmdType != E_WaveCommandType.FX)
//        {
//            TEMP_Logger.Err($"Invalid Type : {cmd.CmdType} , Only {E_WaveCommandType.FX}");
//        }

//        return new WaveFXCmdData(
//            spawnPointID: cmd.IntValue01,
//            fXKey: cmd.StringValue01,
//            delayAfter: cmd.FloatValue01);
//    }
//}
