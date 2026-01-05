using System;
using System.Collections.Generic;
using UnityEngine;
using GameDB;

/// <summary>
/// TODO : 기능 추가할것 ? 잠재적으로 필요할지도 모르니
///     - 두 Grid 가 같은지에 대한 비교
///     - 한 Grid 를 나머지 Grid 에 머지시키기
///         => 런타임에 새로운 마을? 뭐 이런거 딱 출연시킬때 좋을듯 
/// </summary>
public class MapNodeGrid
{
    protected int _width;
    public int Width => _width;
    protected int _height;
    public int Height => _height;

    public int Count => _width * _height;

    protected MapNode[] _mapNodes;
    public MapNode[] MapNodes => _mapNodes;

    public MapNode this[int x, int z]
    {
        get
        {
            if (IsInside(x, z) == false)
            {
                throw new System.Exception($"Out of list range ! | x:{x} z:{z} | Count : {Count} ");
            }

            return _mapNodes[TilePosToIdx(x, z)];
        }
    }

    public MapNode this[int idx]
    {
        get
        {
            return _mapNodes[idx];
        }
    }

    public MapNode this[Vector2Int pos] => this[pos.x, pos.y];

    public void SetGrid(int width, int height)
    {
        _mapNodes = new MapNode[width * height];

        _width = width;
        _height = height;

        for (int i = 0; i < Count; i++)
        {
            // var pos = IdxToTilePos(i);
            _mapNodes[i] = new MapNode((ushort)i, E_TileStatusFlags.Standard_Terrain);
        }
    }

    public Vector3 TilePosToWorldPos(Vector2Int pos)
    {
        return MapUtils.TilePosToWorldPos(pos);
    }

    public Vector2Int IdxToTilePos(int idx)
    {
        return MapUtils.IdxToTilePos(idx, _width);
    }

    public int TilePosToIdx(int x, int z)
    {
        return MapUtils.TilePosToIdx(x, z, _width);
    }

    public bool CanPlace(int x, int z, E_EntityFlags placingFlag)
    {
        if (IsInside(x, z) == false)
            return false;

        return MapUtils.CanPlace(this[x, z].StatusFlag, placingFlag);
    }

    public bool IsInside(int idx)
    {
        return idx >= 0 && idx < _mapNodes.Length;
    }

    public bool IsInside(int x, int z)
    {
        return x >= 0 && x < Width && z >= 0 && z < Height;
    }

    public bool IsInside(Vector2Int pos)
    {
        return IsInside(pos.x, pos.y);
    }

    public bool IsInside(Vector3 worldPos)
    {
        return worldPos.x >= 0f && worldPos.x <= Width && worldPos.z >= 0f && worldPos.z <= Height;
    }

    public void SetFlagState(int x, int z, ulong entityId, E_TileStatusFlags flag)
    {
        ref var node = ref _mapNodes[TilePosToIdx(x, z)];

        node.ChangeFlag(flag);
        node.OccupyingEntityID = entityId;
    }
}
