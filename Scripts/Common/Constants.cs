
using System.IO;
using UnityEngine;

public static class Constants
{
    public static class Paths
    {
        public static readonly string TableBaseDirectory = Path.Combine(Application.persistentDataPath, "table");
        public static readonly string TableBinFileDirectory = Path.Combine(TableBaseDirectory, "bin");
        public static readonly string TableMetadataFileName = "table_metadata.json";
        public static readonly string TableMetadataPath = Path.Combine(TableBaseDirectory, TableMetadataFileName);

        public static readonly string MapDataBaseRelativeDirectory = "mapdata";
        public static readonly string MapDataBaseDirectory = Path.Combine(Application.persistentDataPath, MapDataBaseRelativeDirectory);
        public static readonly string MapDataSubDirectoryNameFromMetadata = "data";
        public static readonly string MapDataJsonFileDirectory = Path.Combine(MapDataBaseDirectory, MapDataSubDirectoryNameFromMetadata);
        public static readonly string MapDataMetadataFileName = "map_metadata.json";
        public static readonly string MapDataMetadataPath = Path.Combine(MapDataBaseDirectory, MapDataMetadataFileName);
    }

    public static class UI
    {
        public const float ToastDuration = 1.0f;
        public const float LongToastDuration = 3f;

        public const float NextAutoBubbleRandomMin = 10f;
        public const float NextAutoBubbleRandomMax = 15f;
        public const float AutoBubbleTextDuration = 4f;
    }

    public static class CameraControl
    {
        public const float CameraMoveSpeed = 2f;
        public const float CameraPinchingZoomSensitivity = 50;
        public const float CameraFocusSpeed = 3f;
    }

    public static class InGame
    {
        public const float BattleTimerSeconds = 120;
        // 전투 시작 n 초 이전에 경고
        public const float BattleStartAlertBeforeEnter = 10f;

        // 한번 전투에 타깃팅 개수 
        public const int TargetCount = 6;

        public static readonly Vector2 SkillExecutionIconOffset = new Vector2(0, 330);

        public const float HugeImapctThreshold = 7f;
        public const float SqrImpactImpulsePercetibleRange = 11f * 11f;
    }

    public const float UI_EnterExit_Duration = 0.3f;

    public const float Gravity = 9.81f;

    public const uint ButtonClickID = 0;

    // 셀 사이즈 (월드 기준)
    //  => 정말 만약에 나중에 이 CellSize 가 바뀌어야 할 일이 있을지도 모르니 일단 변수로 빼서 작업하자 . 
    public const int MapNodeCellSize = 1;
    public const float MapNodeCellHalfSize = MapNodeCellSize * 0.5f;
}
