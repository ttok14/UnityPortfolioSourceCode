using GameDB;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EntityOccupationData : EntityDataBase
{
    const int NeighborSectorCount = 10;
    const float NeighborSectorAngle = 360f / NeighborSectorCount;

    private HashSet<Vector2Int> _occupyingTiles;
    public HashSet<Vector2Int> OccupyingTilePositions => _occupyingTiles;

    private HashSet<Vector2Int> _groundWalkableNeighborNodes;
    public HashSet<Vector2Int> GroundWalkableNeighborPositions => _groundWalkableNeighborNodes;

    private List<Vector2Int> _groundWalkableNeighborNodeList;
    public IReadOnlyList<Vector2Int> GroundWalkableNeighborPositionList => _groundWalkableNeighborNodeList;

    private List<Vector2Int>[] _groundWalkableNeighborSectorList;

    /// <summary>
    /// 참고로 <paramref name="moverFlag"/> 에 <see cref="E_EntityFlags.None"/> 을 넣으면
    /// '점유 가능' 은 무시한 채로 위치를 계산
    /// </summary>
    public bool GetClosestPositionFrom(Vector3 from, E_EntityFlags moverFlag, out Vector3 resultPos)
    {
        resultPos = default;

        if (_groundWalkableNeighborSectorList == null)
            return false;

        // 스타트는 From 으로부터 가장 가까운 Sector 에서 시작
        int centerIdx = PositionToSectorIndex(MapUtils.WorldPosToTilePos(from), _owner.ApproxPosition);
        int arrLength = _groundWalkableNeighborSectorList.Length;
        int loopCnt = (arrLength / 2) + 1;

        // 양방향 순회 
        for (int i = 0; i < loopCnt; i++)
        {
            int leftIdx = (centerIdx - i + arrLength) % arrLength;
            int rightIdx = (centerIdx + i) % arrLength;

            // 처음일때 또는 완전히 정반대에 위치한 경우(하나 남음)
            if (i == 0 || leftIdx == rightIdx)
            {
                if (TrySearchShortestPosition(from, _groundWalkableNeighborSectorList[rightIdx], moverFlag, out var resTilePos))
                {
                    resultPos = MapUtils.TilePosToWorldPos(resTilePos);
                    return true;
                }
            }
            else
            {
                var leftRes = TrySearchShortestPosition(from, _groundWalkableNeighborSectorList[leftIdx], moverFlag, out var leftResTilePos);
                var rightRes = TrySearchShortestPosition(from, _groundWalkableNeighborSectorList[rightIdx], moverFlag, out var rightResTilePos);

                if (leftRes && rightRes)
                {
                    var fromTilePos = MapUtils.WorldPosToTilePos(from);
                    if (MapUtils.GetDistance(fromTilePos, leftResTilePos) < MapUtils.GetDistance(fromTilePos, rightResTilePos))
                        resultPos = MapUtils.TilePosToWorldPos(leftResTilePos);
                    else resultPos = MapUtils.TilePosToWorldPos(rightResTilePos);

                    return true;
                }

                if (leftRes)
                {
                    resultPos = MapUtils.TilePosToWorldPos(leftResTilePos);
                    return true;
                }

                if (rightRes)
                {
                    resultPos = MapUtils.TilePosToWorldPos(rightResTilePos);
                    return true;
                }
            }
        }
        return false;
    }

    public bool GetRandomAccessibleSectorPosition(Vector3 from, E_EntityFlags moverFlag, out Vector3 resultPos)
    {
        resultPos = default;
        if (_groundWalkableNeighborSectorList == null)
        {
            return false;
        }

        int bestSectorIdx = PositionToSectorIndex(MapUtils.WorldPosToTilePos(from), _owner.ApproxPosition);
        bool isSuccess = TryGetRandomPosInSector(bestSectorIdx, moverFlag, out var resTilePos);
        if (isSuccess)
        {
            resultPos = MapUtils.TilePosToWorldPos(resTilePos);
            return true;
        }

        /// 사방이 막힌거임? 이 상황은 나오면 안됌 
        TEMP_Logger.Err($"Failed to get RandomAccessibleSectorPosition , Consider this as an error");

        return false;
    }

    bool TryGetRandomPosInSector(int sectorIdx, E_EntityFlags moverFlag, out Vector2Int resultPos)
    {
        var list = _groundWalkableNeighborSectorList[sectorIdx];

        if (TryWalkableRandomSearchList(list, moverFlag, out resultPos))
            return true;

        int leftSectorIdx = sectorIdx > 0 ? sectorIdx - 1 : _groundWalkableNeighborSectorList.Length - 1;
        int rightSectorIdx = sectorIdx < _groundWalkableNeighborSectorList.Length - 1 ? sectorIdx + 1 : 0;

        int firstIdx;
        int secondIdx;
        if (UnityEngine.Random.Range(0, 100) < 50)
        {
            firstIdx = leftSectorIdx;
            secondIdx = rightSectorIdx;
        }
        else
        {
            firstIdx = rightSectorIdx;
            secondIdx = leftSectorIdx;
        }

        list = _groundWalkableNeighborSectorList[firstIdx];

        if (TryWalkableRandomSearchList(list, moverFlag, out resultPos))
            return true;

        list = _groundWalkableNeighborSectorList[secondIdx];

        if (TryWalkableRandomSearchList(list, moverFlag, out resultPos))
            return true;

        if (TryWalkableRandomSearchList(_groundWalkableNeighborNodeList, moverFlag, out resultPos))
            return true;

        resultPos = default;
        return false;
    }

    bool TryWalkableRandomSearchList(List<Vector2Int> list, E_EntityFlags moverFlag, out Vector2Int resultPos)
    {
        if (list == null || list.Count == 0)
        {
            resultPos = default;
            return false;
        }

        // 랜덤성을 위해, 시작 인덱스를 랜덤으로 뽑음
        int startIdx = UnityEngine.Random.Range(0, list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            var pos = list[(startIdx + i) % list.Count];

            if (MapManager.Instance.CanPlace(pos, moverFlag))
            {
                resultPos = pos;
                return true;
            }
        }

        resultPos = default;
        return false;
    }

    bool TrySearchShortestPosition(Vector3 from, List<Vector2Int> list, E_EntityFlags moverFlag, out Vector2Int resultPos)
    {
        resultPos = default;

        if (list == null || list.Count == 0)
        {
            return false;
        }

        int shortestDist = int.MaxValue;
        bool has = false;

        for (int i = 0; i < list.Count; i++)
        {
            if (moverFlag == E_EntityFlags.None || MapManager.Instance.CanPlace(list[i], moverFlag))
            {
                int dist = MapUtils.GetDistance(list[i], MapUtils.WorldPosToTilePos(from));

                if (dist < shortestDist)
                {
                    has = true;

                    shortestDist = dist;
                    resultPos = list[i];
                }
            }
        }

        return has;
    }


    public IReadOnlyList<Vector3> GroundWalkableNeighborWorldPositionList =>
        _groundWalkableNeighborNodes.Select(t => MapUtils.TilePosToWorldPos(t)).ToList();

    public override void OnPoolActivated(IInstancePoolInitData initData)
    {
        base.OnPoolActivated(initData);

        Refresh();
    }

    public override void OnPoolReturned()
    {
        base.OnPoolReturned();

        if (_occupyingTiles != null)
            _occupyingTiles.Clear();
        if (_groundWalkableNeighborNodes != null)
            _groundWalkableNeighborNodes.Clear();
        if (_groundWalkableNeighborNodeList != null)
            _groundWalkableNeighborNodeList.Clear();
    }

    public override void ReturnToPool()
    {
        InGameManager.Instance.CacheContainer.EntityDataPool.Return(this);
    }

    // 무거운 작업임. 한번 맵에 박히고 특별한거 없 으면 굳이 호출할일 없을듯.
    // (기존 neighbor 노드의 walkable 에 영향이 갈만하 작업이 아니면은... )
    public void Refresh()
    {
        RefreshOccupyingTiles();
        RefreshWalkableNeighborTiles();
        RefreshWalkableNeighborSectors();
    }

    private void RefreshOccupyingTiles()
    {
        var localOccupationPoints = TableData.OccupyOffsets;
        if (localOccupationPoints == null)
            return;

        if (_occupyingTiles == null)
            _occupyingTiles = new HashSet<Vector2Int>();
        else _occupyingTiles.Clear();

        var ownerPos = _owner.ApproxPosition;
        var ownerEulerY = _owner.transform.eulerAngles.y;

        for (int i = 0; i < localOccupationPoints.Length; i++)
        {
            var occupyWorldPos = MapUtils.TransformTilePosToWorldPos(localOccupationPoints[i], ownerPos, ownerEulerY);
            if (MapManager.Instance.IsInside(occupyWorldPos) == false)
            {
                continue;
            }

            _occupyingTiles.Add(MapUtils.WorldPosToTilePos(occupyWorldPos));
        }
    }

    private void RefreshWalkableNeighborTiles()
    {
        if (_occupyingTiles == null)
            return;

        if (_groundWalkableNeighborNodes == null)
            _groundWalkableNeighborNodes = new HashSet<Vector2Int>();
        else _groundWalkableNeighborNodes.Clear();

        if (_groundWalkableNeighborNodeList == null)
            _groundWalkableNeighborNodeList = new List<Vector2Int>();
        else _groundWalkableNeighborNodeList.Clear();

        var entityTilePos = MapUtils.WorldPosToTilePos(_owner.ApproxPosition);

        MapUtils.ForeachNeighbors(_occupyingTiles, (pos) =>
        {
            if (_occupyingTiles.Contains(pos) || pos == entityTilePos)
                return;

            if (MapManager.Instance.CanPlace(pos, TableData.EntityFlags))
            {
                _groundWalkableNeighborNodes.Add(pos);
                _groundWalkableNeighborNodeList.Add(pos);
            }
        });
    }

    private void RefreshWalkableNeighborSectors()
    {
        if (_occupyingTiles == null || _groundWalkableNeighborNodeList == null)
            return;

        if (_groundWalkableNeighborSectorList == null)
        {
            _groundWalkableNeighborSectorList = new List<Vector2Int>[NeighborSectorCount];
            for (int i = 0; i < NeighborSectorCount; i++)
            {
                _groundWalkableNeighborSectorList[i] = new List<Vector2Int>(4);
            }
        }
        else
        {
            for (int i = 0; i < _groundWalkableNeighborSectorList.Length; i++)
            {
                _groundWalkableNeighborSectorList[i].Clear();
            }
        }

        var ownerPos = _owner.ApproxPosition.FlatHeight();

        for (int i = 0; i < _groundWalkableNeighborNodeList.Count; i++)
        {
            int idx = PositionToSectorIndex(_groundWalkableNeighborNodeList[i], ownerPos);
            _groundWalkableNeighborSectorList[idx].Add(_groundWalkableNeighborNodeList[i]);

            //GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //go.transform.position = MapUtils.TilePosToWorldPos(_groundWalkableNeighborNodeList[i]);
            //go.name = $"SectorIndex: {idx}";
        }
    }

    int PositionToSectorIndex(Vector2Int pos, Vector3 ownerPos)
    {
        // Owner 에서 pos 로 향하는 방향 구함 
        Vector3 direction = MapUtils.TilePosToWorldPos(pos) - ownerPos;
        // atan2 로 0~180 -180~0도 까지 구함 
        float degree = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg;

        // 음수면 360 도를 더해서 0~360 도로 Clamp 
        if (degree < 0)
            degree += 360;

        // NeighborSectorCount 개수 배열에 맞게 인덱스 계산
        return Mathf.FloorToInt((degree + (NeighborSectorAngle * 0.5f)) / NeighborSectorAngle) % NeighborSectorCount;
    }
}
