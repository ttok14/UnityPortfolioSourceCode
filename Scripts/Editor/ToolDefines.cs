using System;
using UnityEditor;

namespace Tool
{

    [System.Flags]
    public enum GameObjectSelectionFlags
    {
        None = 0,

        OnlyPrefabRoot,
        PartOfPrefab,
    }
}
