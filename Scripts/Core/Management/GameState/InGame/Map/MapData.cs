using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MapData
{
    public uint id;
    // .json 파일 명과도 일치해야함 . (IO 작업땜시)
    public string name;

    public int width;
    public int height;

    public Vector2Int activationLeftBottomMapPosition;
    public Vector2Int activationRightTopMapPosition;

    public string terrainMaterialKey;

    public List<Vector2Int> enemySpawnPositions;

    public List<EntityObjectData> objects;

    public MapData Copy(bool deepCopyObjects = true)
    {
        var clone = (MapData)MemberwiseClone();

        if (deepCopyObjects)
        {
            clone.enemySpawnPositions = new List<Vector2Int>(enemySpawnPositions);
            clone.objects = new List<EntityObjectData>(objects.Count);
            foreach (var obj in objects)
            {
                clone.objects.Add(obj.Copy());
            }
        }
        return clone;
    }
}
