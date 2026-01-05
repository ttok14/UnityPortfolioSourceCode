using UnityEngine;
using GameDB;

// 현 겜에는 로컬라이제이션 별도로 안넣을거기 때문에
// eng->kor 시스템이 없음 하지만
/// <see cref="E_StructureType"/> 같은 애들을 문자열로
/// 표현해주어야 할때는 일부 비스무리한 기능이 필요하므로
/// 이 부분 간단하게라도 수행해줄 클래스가 필요
public static class TempLocale
{
    public static string To(E_StructureType type)
    {
        switch (type)
        {
            case E_StructureType.None:
                return "없음";
            case E_StructureType.Residential:
                return "마을 회관";
            case E_StructureType.Defense:
                return "공격타워";
            case E_StructureType.Spawner:
                return "유닛 생성소";
            case E_StructureType.Storage:
                return "재화 창고";
            case E_StructureType.ResourceGenerator:
                return "재화 생성소";
            default:
                return "없음";
        }
    }
}
