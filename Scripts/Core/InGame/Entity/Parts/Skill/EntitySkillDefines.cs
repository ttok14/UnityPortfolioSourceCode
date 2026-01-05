using UnityEngine;

public struct EntitySkillTriggerContext
{
    public int SlotIdx;

    // ** 어느시점이던 Executor / Target / StartSocket 같은
    // 참조 형식들은 Valid 체크를 해줘야함 . 유닛이면 찰나에 죽을수도 있는거고
    // Release 가 되어버릴수도있는거 **
    public EntityBase Executor;
    public EntityBase Target;

    // 스킬 projectile 등이 발사되거나 이펙트가 발동될 Model 의 Equipment
    // TODO : 근데 애를 밖에서 정해주는게 맞나? ......
    public EntityEquipmentType SkillEquipment;

    // Only Spell 용
    public EntitySpellTriggerContext SpellContext;

    // public EntityModelSocket ProjectileStartSocketType;

    // public Vector3 FixedPoint;
}

public struct EntitySpellTriggerContext
{
    //public Vector3 Point;
}
