using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameDB;
using System.Threading;

public static class MapUtils
{
    //const int LineNodeCost = 10;
    //const int DiagonalNodeCost = 14;

    public static List<Vector2Int> GetTilePosMatchingWorldPosition(
        Vector3 worldPosition,
        int yEulerRot,
        Vector2Int[] localTilePositions)
    {
        Vector3[] worldOffsets = localTilePositions.Select(t => new Vector3(t.x, 0, t.y)).ToArray();
        var rot = Quaternion.Euler(0, yEulerRot, 0);
        List<Vector2Int> result = new List<Vector2Int>();
        HashSet<Vector2Int> duplicateCheck = new HashSet<Vector2Int>();

        for (int i = 0; i < worldOffsets.Length; i++)
        {
            worldOffsets[i] = (rot * worldOffsets[i]) + worldPosition;
            var toTilePos = WorldPosToTilePos(worldOffsets[i]);
            if (duplicateCheck.Contains(toTilePos))
                continue;
            result.Add(toTilePos);

            // foreach (var overlappedPos in GetOverlappingTilePos(new Rect(new Vector2(worldOffsets[i].x - 0.5f, worldOffsets[i].z - 0.5f), new Vector2(1, 1))))
            //if (duplicateCheck.Contains(overlappedPos))
            //  continue;
        }

        return result;
    }

    //public static Vector2Int[] GetOverlappingTilePos(Vector3 position)
    //{

    //}

    //public static void ApplyEntityObjectFlagToGrid(MapNodeGrid grid, EntityObjectData mapEntityData)
    //{
    //    var tilePos = WorldPosToTilePos(mapEntityData.worldPosition);
    //    if (grid.IsInside(tilePos.x, tilePos.y) == false)
    //    {
    //        return;
    //    }

    //    var tableData = DBEntity.Get(mapEntityData.entityId);
    //    if (tableData == null)
    //    {
    //        TEMP_Logger.Err($"Given EntityDataID is not valid : {mapEntityData.entityId}");
    //        return;
    //    }

    //    grid.SetFlagState(tilePos.x, tilePos.y, ModifiedTileStatusByFlags(grid[tilePos].StatusFlag, tableData.EntityFlags, true));
    //}

    public static void ApplyEntityFlagToGrid(MapNodeGrid grid, bool placedOrDestroyed, EntityBase entity)
    {
        var tilePos = WorldPosToTilePos(entity.ApproxPosition);
        if (grid.IsInside(tilePos.x, tilePos.y) == false)
            return;

        var occupationData = entity.GetData<EntityOccupationData>();
        if (occupationData == null)
            return;

        foreach (var pos in occupationData.OccupyingTilePositions)
        {
            if (grid.IsInside(pos) == false)
                continue;

            grid.SetFlagState(
                pos.x,
                pos.y,
                // 설치일때는 occupier 를 넘겨주지만 아니라면 0 으로 (walkable 과 상관없이 하자 일단..)
                placedOrDestroyed ? entity.ID : 0,
                ModifiedTileStatusByFlags(grid[pos].StatusFlag, entity.TableData.EntityFlags, placedOrDestroyed));
        }
    }

    //public static void ApplyEntityFlagToGrid(MapNodeGrid grid, List<EntityObjectData> mapObjectDataList)
    //{
    //    foreach (var data in mapObjectDataList)
    //    {
    //        var tilePos = WorldPosToTilePos(data.worldPosition);
    //        if (grid.IsInside(tilePos.x, tilePos.y) == false)
    //        {
    //            continue;
    //        }

    //        var tableData = DBEntity.Get(data.entityId);
    //        if (tableData == null)
    //        {
    //            TEMP_Logger.Err($"Given MapObjectDataID is not valid : {data.entityId}");
    //            continue;
    //        }

    //        var localOccupationPoints = tableData.OccupyOffsets;
    //        if (localOccupationPoints != null)
    //        {
    //            for (int i = 0; i < localOccupationPoints.Length; i++)
    //            {
    //                var occupyPosVec3 = new Vector3(localOccupationPoints[i].x, 0, localOccupationPoints[i].y);
    //                var occupyWorldPos = Quaternion.Euler(0, data.eulerRotY, 0) * occupyPosVec3 + data.worldPosition;
    //                if (grid.IsInside(occupyWorldPos) == false)
    //                {
    //                    continue;
    //                }

    //                var toTilePos = grid.WorldPosToTilePos(occupyWorldPos);
    //                grid.SetFlagState(toTilePos.x, toTilePos.y, ModifiedTileStatusByFlags(grid[toTilePos].StatusFlag, tableData.EntityFlags, true));
    //            }
    //        }
    //    }
    //}

    public static int GetDistance(Vector2Int from, Vector2Int to)
    {
        int xDist = to.x - from.x;
        int yDist = to.y - from.y;

        return (xDist * xDist) + (yDist * yDist);
    }

    public static Vector2Int IdxToTilePos(int idx, int width)
    {
        return new Vector2Int(idx % width, idx / width);
    }

    public static Vector3 IdxToWorldPos(int idx, int width)
    {
        return TilePosToWorldPos(idx % width, idx / width);
    }

    public static int TilePosToIdx(Vector2Int pos, int width)
    {
        return width * pos.y + pos.x;
    }

    public static int TilePosToIdx(int x, int z, int width)
    {
        return width * z + x;
    }

    public static Vector3 TilePosToWorldPos(Vector2Int pos)
    {
        return new Vector3(0.5f + pos.x, 0, 0.5f + pos.y);
    }

    public static Vector3 TilePosToWorldPos(int x, int z)
    {
        return new Vector3(0.5f + x, 0, 0.5f + z);
    }

    public static Vector3 WorldPosToTileWorldPos(Vector3 pos)
    {
        var tilePos = WorldPosToTilePos(pos);
        return new Vector3(tilePos.x + 0.5f, 0, tilePos.y + 0.5f);
    }

    public static Vector2Int WorldPosToTilePos(Vector3 pos)
    {
        return new Vector2Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.z));
    }

    public static int WorldPosToIdx(Vector3 pos, int width)
    {
        return TilePosToIdx(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.z), width);
    }

    public static Vector3 TransformTilePosToWorldPos(Vector2Int localTilePos, Vector3 transformPos, float transformEulerY)
    {
        var worldPos = TilePosToWorldPos(localTilePos);
        return Quaternion.Euler(0, transformEulerY, 0) * worldPos + transformPos;
    }

    public static Vector2Int TransformTilePos(Vector2Int localTilePos, Vector3 transformPos, float transformEulerY)
    {
        var worldPos = TilePosToWorldPos(localTilePos);
        var transformedWorldPos = Quaternion.Euler(0, transformEulerY, 0) * worldPos + transformPos;
        return WorldPosToTilePos(transformedWorldPos);
    }

    public static Vector3 SomewhereInTilePos(Vector2Int pos)
    {
        return new Vector3(pos.x + UnityEngine.Random.Range(-0.5f, 0.5f), 0, pos.y + UnityEngine.Random.Range(-0.5f, 0.5f));
    }

    public static Vector3 SomewhereInTilePos(Vector3 pos)
    {
        return SomewhereInTilePos(WorldPosToTilePos(pos));
    }

    public static bool IsInsideArea(Vector2Int pos, Vector2Int leftBot, Vector2Int rightTop)
    {
        return pos.x >= leftBot.x &&
            pos.x <= rightTop.x &&
            pos.y >= leftBot.y &&
            pos.y <= rightTop.y;
    }

    public static Vector3? ScreenPosToWorldPos(Camera camera, Vector3 screenPos)
    {
        var ray = camera.ScreenPointToRay(screenPos);
        var plane = new Plane(Vector3.up, Vector3.zero);
        if (plane.Raycast(ray, out var enter))
        {
            return ray.GetPoint(enter);
        }
        return null;
    }

    public static Vector2Int ScreenPosToTilePos(Vector3 screenPos)
    {
        var ray = CameraManager.Instance.MainCam.ScreenPointToRay(screenPos);
        var plane = new Plane(Vector3.up, Vector3.zero);
        plane.Raycast(ray, out var enter);
        return WorldPosToTilePos(screenPos);
    }

    public static Vector2Int[] GetNeighborPos(Vector2Int pos, Predicate<Vector2Int> pickerFilter = null)
    {
        Vector2Int[] neighbors = new Vector2Int[8];
        int startX = pos.x - 1;
        int startZ = pos.y - 1;
        int sizeForTwoSides = 3;

        for (int x = 0; x < sizeForTwoSides; x++)
        {
            for (int z = 0; z < sizeForTwoSides; z++)
            {
                int posX = startX + x;
                int posZ = startZ + z;

                if (posX == pos.x && posZ == pos.y)
                    continue;

                int idx = z * sizeForTwoSides + x;

                if (pickerFilter != null)
                {
                    if (pickerFilter(new Vector2Int(x, z)))
                    {
                        neighbors[idx] = new Vector2Int(posX, posZ);
                    }
                }
                else
                {
                    neighbors[idx] = new Vector2Int(posX, posZ);
                }
            }
        }

        return neighbors;
    }

    public static bool CanPlace(E_TileStatusFlags tileStatusFlag, E_EntityFlags placingObjectFlag)
    {
        if (tileStatusFlag.HasFlag(E_TileStatusFlags.Unable))
            return false;

        // 오브젝트가 GroundWalkable 만 지날 수 있다면
        // 타일의 현재 상태가 GroundWalkable 일때 지날 수 있다 
        if (placingObjectFlag.HasFlag(E_EntityFlags.Requires_Walkable_Ground) &&
            tileStatusFlag.HasFlag(E_TileStatusFlags.Walkable_Ground) == false)
            return false;

        // 오브젝트가 AirWalkable 만 지날수 있다면
        // 타일의 현재 상태가 AirWalkable 일때 지날수있다 
        if (placingObjectFlag.HasFlag(E_EntityFlags.Require_Walkable_Air) &&
            tileStatusFlag.HasFlag(E_TileStatusFlags.Walkable_Air) == false)
            return false;

        // 오브젝트가 Jumpable 만 지날 수 있다면
        // 타일의 현재 상태도 Jumpable 일때만 지날 수 있다 
        if (placingObjectFlag.HasFlag(E_EntityFlags.Require_Jumpable) &&
            tileStatusFlag.HasFlag(E_TileStatusFlags.Jumpable_Ground) == false)
            return false;

        return true;
    }

    public static E_TileStatusFlags ModifiedTileStatusByFlags(E_TileStatusFlags tileFlag, E_EntityFlags placedEntityFlag, bool placedOrDestroyed)
    {
        if (placedEntityFlag.HasFlag(E_EntityFlags.Block_Ground_Movement))
        {
            if (placedOrDestroyed)
                tileFlag = (E_TileStatusFlags)EnumFlagUtil.Remove((int)tileFlag, (int)E_TileStatusFlags.Walkable_Ground);
            else tileFlag = (E_TileStatusFlags)EnumFlagUtil.Add((int)tileFlag, (int)E_TileStatusFlags.Walkable_Ground);
        }

        if (placedEntityFlag.HasFlag(E_EntityFlags.Block_Air_Movement))
        {
            if (placedOrDestroyed)
                tileFlag = (E_TileStatusFlags)EnumFlagUtil.Remove((int)tileFlag, (int)E_TileStatusFlags.Walkable_Air);
            else tileFlag = (E_TileStatusFlags)EnumFlagUtil.Add((int)tileFlag, (int)E_TileStatusFlags.Walkable_Air);
        }

        return tileFlag;
    }

    public static HashSet<Vector2Int> GetTilesOnPath(List<Vector3> pathPoints, HashSet<Vector2Int> result)
    {
        if (result == null)
            result = new HashSet<Vector2Int>();

        if (pathPoints == null || pathPoints.Count < 2)
            return result;

        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Vector3 startPos = pathPoints[i];
            Vector3 endPos = pathPoints[i + 1];

            // 각 선분마다 HashSet에 타일 추가 (이미 있으면 무시됨)
            GetTilesOnLine(startPos, endPos, result);
        }

        return result;
    }

    public static void GetTilesOnLine(Vector3 startPos, Vector3 endPos, HashSet<Vector2Int> resultSet)
    {
        Vector2Int startTile = WorldPosToTilePos(startPos);
        Vector2Int endTile = WorldPosToTilePos(endPos);

        GetTilesOnLine(startTile, endTile, resultSet);
    }

    // [AI 작성 - 브레젠험 알고리즘 (라인상 타일들)]
    public static void GetTilesOnLine(Vector2Int start, Vector2Int end, HashSet<Vector2Int> resultSet)
    {
        if (resultSet == null)
            return;

        int x = start.x;
        int y = start.y;
        int x2 = end.x;
        int y2 = end.y;

        int w = x2 - x;
        int h = y2 - y;

        int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;

        if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
        if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
        if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;

        int longest = Mathf.Abs(w);
        int shortest = Mathf.Abs(h);

        if (!(longest > shortest))
        {
            longest = Mathf.Abs(h);
            shortest = Mathf.Abs(w);
            if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
            dx2 = 0;
        }

        int numerator = longest >> 1;

        for (int i = 0; i <= longest; i++)
        {
            // List.Add 대신 HashSet.Add 사용 (중복이면 false 반환하며 무시됨)
            resultSet.Add(new Vector2Int(x, y));

            numerator += shortest;
            if (!(numerator < longest))
            {
                numerator -= longest;
                x += dx1;
                y += dy1;
            }
            else
            {
                x += dx2;
                y += dy2;
            }
        }
    }

    static HashSet<Vector2Int> _internalHashSetCache = new HashSet<Vector2Int>();
    // [AI 작성 - 브레젠험 알고리즘 (라인상 타일들)]
    /// <summary> callbackPerTile 결과로 False 받으면 곧 바로 탈출  </summary>
    public static void ProcessCallBackTilesOnLine(Vector2Int start, Vector2Int end, Func<Vector2Int, bool> callbackPerTile)
    {
        if (callbackPerTile == null)
            return;

        _internalHashSetCache.Clear();

        int x = start.x;
        int y = start.y;
        int x2 = end.x;
        int y2 = end.y;

        int w = x2 - x;
        int h = y2 - y;

        int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;

        if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
        if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
        if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;

        int longest = Mathf.Abs(w);
        int shortest = Mathf.Abs(h);

        if (!(longest > shortest))
        {
            longest = Mathf.Abs(h);
            shortest = Mathf.Abs(w);
            if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
            dx2 = 0;
        }

        int numerator = longest >> 1;

        for (int i = 0; i <= longest; i++)
        {
            var pos = new Vector2Int(x, y);
            if (_internalHashSetCache.Add(pos) == false)
                continue;

            // False 받으면 곧바로 탈출 정책
            if (callbackPerTile.Invoke(pos) == false)
                return;

            numerator += shortest;
            if (!(numerator < longest))
            {
                numerator -= longest;
                x += dx1;
                y += dy1;
            }
            else
            {
                x += dx2;
                y += dy2;
            }
        }
    }

    // 매번 가비지 생성을 막기위함. hashset 레퍼런스 보유해서 gc안되게 막음.
    // clear() 야 어차피 내부 배열 할당된 요소를 삭제하는게 아니기땜에 gc 안됨.
    // 게다가 vector2int 값타입이라 힙할당도없음
    // (싱글쓰레드에서만 안전 참고..)
    static HashSet<Vector2Int> _foreachDoneListCache = new HashSet<Vector2Int>();
    public static void ForeachNeighbors(IEnumerable<Vector2Int> tilePositions, Action<Vector2Int> onFind)
    {
        foreach (var pos in tilePositions)
        {
            int startX = pos.x - 1;
            int startZ = pos.y - 1;
            int sizeForTwoSides = 3;

            for (int x = 0; x < sizeForTwoSides; x++)
            {
                for (int z = 0; z < sizeForTwoSides; z++)
                {
                    Vector2Int p = new Vector2Int(startX + x, startZ + z);

                    // neighbor 아님 
                    if (p.x == pos.x && p.y == pos.y)
                        continue;

                    if (_foreachDoneListCache.Contains(p))
                        continue;

                    onFind.Invoke(p);
                    _foreachDoneListCache.Add(p);
                }
            }
        }

        _foreachDoneListCache.Clear();
    }
}
