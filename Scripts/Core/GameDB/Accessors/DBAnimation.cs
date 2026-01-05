
using GameDB;

[Attribute_GameDBAccessor()]
public class DBAnimation
{
    public static AnimationTable Get(uint id)
    {
        if (GameDBManager.Instance.Container.AnimationTable_data.TryGetValue(id, out var data) == false)
        {
            return null;
        }
        return data;
    }

    public static void OnTableReady()
    {
    }

    public static void Release()
    {
    }
}
