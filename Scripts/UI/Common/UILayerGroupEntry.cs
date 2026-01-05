using System;
using UnityEngine;

[Serializable]
public class UILayerGroupEntry
{
    public Canvas canvas;
    public UILayer layer;

    public UILayerGroupEntry Clone() => MemberwiseClone() as UILayerGroupEntry;
}
