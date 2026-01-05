using System;
using UnityEngine;
using GameDB;

[System.Flags]
public enum E_TileStatusFlags
{
    None = 0,

    // 타일위에 지상 Walkable 여부
    //  => 타일 위에 있는 물체가 '건물' 이다 그럼 , 건물은
    //      타일의 이 Walkable 값을 막아버리는 속성을 가지고 있겠지?
    //  => 또 생각해봐야 할거 , 이 값은, '지상 물체가 위치할 수 없다' 의 Standard 한
    //      방식의 표현임 . 즉, 예를들어, 지상 물체이지만 , 투명한 귀신이 있을수도 있 는거.
    //      애는 벽뚫하는 유닛일 수도 있는거고. 그렇기 때문에 타일위에 '위치한다' 라는 것은
    //      그 물체가 가지고 있는 어떤 위치에 관련된 속성 + 타일위의 속성 이 합쳐졌을때 최종
    //      결정이 됨. 
    Walkable_Ground = 0x1,
    // 타일위에 공기중 Walkable 여부 
    Walkable_Air = 0x1 << 2,
    // 점프가 가능한 Jumpable 여부
    //  => 높이가 낮은 Fence 같은 경우는, Walkalble 에 의해 막힌 타일이 있다면
    //      이벤트성 유닛 (e.g 닌자?)이 그냥 훌쩍 넘을수도 있이어야한다.
    //      => 그렇기 때문에, 현재 타일이 막혀는 있지만, Jump 가 가능한 유닛은 여길 넘을 수 있단 플래그값
    Jumpable_Ground = 0x1 << 3,

    // 그냥 막아버림 , 현재 최적화위함
    Unable = 0x1 << 4,


    //// => 타일 위에 위치한 지상 물체가 있는가?
    ////      => 이는 , 지상 물체가 있다고 해서 Walkable 이 Block 이 되는것은 아님.
    ////          이런 이유로 분리가 돼있는것.
    ////          이 물체의 속성에 따라 Walkable 에 영향을 주는 것. 막말로 지상 물체가 두개가
    ////          겹칠 수 도 있는것임. 첫번쨰로 위치한 물체가 Walkable 에 영향을 주지않는다면 .
    //Occupied_Ground = 0x1 << 4,
    //// => 타일 위에 위치한 공중 물체가 있는가 ?
    //Occupied_Air = 0x1 << 5,

    // 프리셋
    Standard_Terrain = Walkable_Ground | Jumpable_Ground | Walkable_Air,
}

//[System.Flags]
//public enum E_ObjectFlags
//{
//    None = 0,

//    #region ===:: 배치 관련 정보 ::===
//    // 지상에 있는 오브젝트 
//    GroundedObject = 0x1,
//    // 날아다니는 오브젝트 
//    FlyingObject = 0x1 << 1,
//    // 점프될 수 있는 오브젝트
//    Jumpable = 0x1 << 2,
//    #endregion

//    #region ===:: 이동을 위한 조건 플래그 ::=== 
//    // 이동을 위해선 지상 경로가 Walkable 이어야한다 
//    Requires_Walkable_Ground = 0x1 << 3,
//    // 이동을 위해선 하늘 경로가 Walkable 이어야한다
//    Require_Walkable_Air = 0x1 << 4,
//    // 이동을 위해선 지상 경로가 Jumpable 이어야한다
//    Require_Jumpable = 0x1 << 5,
//    #endregion

//    #region ===:: 이동시 타일에 미치는 영향 플래그 ::===
//    // 위치한 타일의 ground walkable 을 non walkable 로 만든다 
//    Block_Ground_Movement = 0x1 << 6,
//    // 위치한 타일의 air walkable 을 non walkable 로 만든다
//    Block_Air_Movement = 0x1 << 7,
//    #endregion

//    Require_PlacementCondition = Requires_Walkable_Ground | Require_Walkable_Air | Jumpable,
//}

public interface IMapEntity
{
    public E_EntityFlags Flags { get; }
}

public enum NodeDirection
{
    None = -1,
    LeftTop = 0, Top = 1, RightTop = 2,
    Left = 3, Center = 4, Right = 5,
    LeftBot = 6, Bot = 7, RightBot = 8,

    End = 9
}
