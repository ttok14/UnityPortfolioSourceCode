using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using GameDB;

// TODO : 근데 From 이 CanPlace 가 아닌거는 뭐 어케하지? 가끔 이런 현상있던데 원인 찾아야함

public class DefensePathSystem
{
    List<PathBuffer.PathKey> _pathKeyCache = new List<PathBuffer.PathKey>();

    public void Initialize()
    {
    }

    public bool RemoveOverlappedPaths(IReadOnlyList<Vector2Int> positions /*, E_EntityFlags refreshMoverFlag , CancellationToken ctk*/ )
    {
        var buffer = InGameManager.Instance.CacheContainer.PathBufferCache;

        _pathKeyCache.Clear();

        buffer.GetOverlappedPathKeys(positions, _pathKeyCache);
        if (_pathKeyCache.Count > 0)
        {
            buffer.RemovePath(_pathKeyCache);
        }

        return _pathKeyCache.Count > 0;
    }

    public void OnBattleEnd()
    {
        InGameManager.Instance.CacheContainer.PathBufferCache.ClearAll();
    }

    public void Release()
    {

    }

    public async UniTask<PathListPoolable> GetPathAsync(Vector2Int from, Vector2Int to, E_EntityFlags moverFlag, PathBuffer.Modifier modifier, CancellationToken ctk)
    {
        var path = await GetPathFromToAsync(MapUtils.TilePosToWorldPos(from), MapUtils.TilePosToWorldPos(to), moverFlag, modifier, ctk);

        if (ctk.IsCancellationRequested)
        {
            if (path != null)
                path.ReturnToPool();
            return null;
        }

        return path;
    }

    //public async UniTask<PathListPoolable> GetPathAsync(Vector3 from, Vector3 to, E_EntityFlags moverFlag, PathBuffer.Modifier modifier, CancellationToken ctk)
    //{
    //    var toTilePos = MapUtils.WorldPosToTilePos(to);

    //    if (MapManager.Instance.CanPlace(toTilePos, moverFlag) == false)
    //    {
    //        if (MapManager.Instance.IsInside(to) == false)
    //            return null;

    //        return await GetPathToEntityAsync(from, MapManager.Instance.GetTileOccupierID(toTilePos), moverFlag, modifier, ctk);
    //    }

    //    return await GetPathFromToAsync(from, to, moverFlag, modifier, ctk);
    //}

    public async UniTaskVoid GetPathAsyncCallBack(Vector3 from, Vector3 to, E_EntityFlags moverFlag, PathBuffer.Modifier modifier, CancellationToken ctk, Action<PathListPoolable> onReceived)
    {
        var path = await GetPathFromToAsync(from, to, moverFlag, modifier, ctk);
        if (ctk.IsCancellationRequested)
        {
            if (path != null)
                path.ReturnToPool();
            onReceived.Invoke(null);
            return;
        }
        onReceived.Invoke(path);
    }

    public async UniTask<PathListPoolable> GetPathToEntityAsync(Vector3 from, ulong entityId, E_EntityFlags moverFlag, PathBuffer.Modifier modifier, CancellationToken ctk)
    {
        var entity = EntityManager.Instance.GetEntity(entityId);
        if (EntityHelper.IsValid(entity) == false)
            return null;

        var pathBuffer = InGameManager.Instance.CacheContainer.PathBufferCache;
        Vector3 destPos = entity.ApproxPosition;
        var entityTilePos = MapUtils.WorldPosToTilePos(destPos);
        PathListPoolable pathToDest;

        if (MapManager.Instance.CanPlace(entityTilePos, moverFlag))
        {
            pathToDest = pathBuffer.TryGetPath(new PathBuffer.PathKey(MapUtils.WorldPosToTilePos(from), entityTilePos, moverFlag), modifier);
            if (pathToDest != null)
                return pathToDest;
        }
        else
        {
            bool foundDest = false;

            var occupationData = entity.GetData(EntityDataCategory.Occupation) as EntityOccupationData;
            if (occupationData != null)
            {
                // 새로 구해진 Entity 의 랜덤 섹터 위치
                if (occupationData.GetRandomAccessibleSectorPosition(from, moverFlag, out var resPos))
                {
                    foundDest = true;
                    destPos = resPos;

                    // 캐시로드 재시도
                    pathToDest = pathBuffer.TryGetPath(
                        new PathBuffer.PathKey(MapUtils.WorldPosToTilePos(from), MapUtils.WorldPosToTilePos(resPos), moverFlag),
                        modifier);

                    if (pathToDest != null)
                        return pathToDest;
                }
            }

            if (foundDest == false)
            {
                // 지금 Entity 위치가 NotWalkable 인데
                // 주변 Occupation 도 모두 다 NotWalkalble 이라면 이거는 시스템 자체에서 막아야함. 이 경우는 일단
                // 완전히 실패 처리.
                TEMP_Logger.Err($"Dest Pos doesnt have proper Occupier. This case is not handled yet....");

                return null;
            }
        }

        destPos.y = 0;

        pathToDest = await pathBuffer.GetPath(from, destPos, moverFlag, modifier, ctk);

        if (ctk.IsCancellationRequested || pathToDest == null || pathToDest.Instance.Count == 0)
        {
            pathToDest?.ReturnToPool();

            return null;
        }

        return pathToDest;
    }

    public async UniTaskVoid GetPathToEntityAsyncCallBack(Vector3 from, ulong entityId, E_EntityFlags moverFlag, PathBuffer.Modifier modifier, CancellationToken ctk, Action<PathListPoolable> onReceived)
    {
        var path = await GetPathToEntityAsync(from, entityId, moverFlag, modifier, ctk);
        if (ctk.IsCancellationRequested)
        {
            if (path != null)
                path.ReturnToPool();

            onReceived.Invoke(null);
            return;
        }
        onReceived.Invoke(path);
    }

    private async UniTask<PathListPoolable> GetPathFromToAsync(Vector3 from, Vector3 to, E_EntityFlags moverFlag, PathBuffer.Modifier modifier, CancellationToken ctk)
    {
        var pathBuffer = InGameManager.Instance.CacheContainer.PathBufferCache;
        var toTilePos = MapUtils.WorldPosToTilePos(to);
        PathListPoolable pathToDest;

        // 도착지가 이동 가능한가 (단일 타일만 검사 주의)
        if (MapManager.Instance.CanPlace(toTilePos, moverFlag))
        {
            // 캐시 검사
            pathToDest = pathBuffer.TryGetPath(new PathBuffer.PathKey(MapUtils.WorldPosToTilePos(from), toTilePos, moverFlag), modifier);

            if (pathToDest != null)
                return pathToDest;
        }
        // 이동이 불가한가 
        else
        {
            if (MapManager.Instance.IsInside(to) == false)
                return null;

            // 이동 불가하면, 그 타일을 점유중인 Entity 를 가져와서 재시도
            ulong occupierId = MapManager.Instance.GetTileOccupierID(toTilePos);
            if (occupierId != 0)
            {
                var res = await GetPathToEntityAsync(from, MapManager.Instance.GetTileOccupierID(toTilePos), moverFlag, modifier, ctk);
                if (ctk.IsCancellationRequested)
                {
                    if (res != null)
                        res.ReturnToPool();
                    return null;
                }

                return res;
            }
            // 이 케이스는 , 가고자하는 타일이 Not Walkable 인데 , Not Walkable 인 이유가
            // Structure 같은 Occupier 가 점유해서가 아니라 , (점유중이면 OccupationData 로 가장 가까운 위치롤 선정해 가져오거나
            // 대응이 가능)
            // Envirionment 같은 현재 최적화를 위해 별도로 생성만 하고 Occupation 은 관리중이지 않은 위치가 들어온 경우
            // 이 경우는 일단 현재 에러라기보다는 코드에서 대응이 안되는 부분.
            // 이 케이스가 들어온다면 애초에 현재 Environment 같은게 최초 생성후 건들이지 않는데 그 위치를 지금 Dest 로 찍고
            // 이동하려고 하는 컨텐츠 부분 자체를 수정해야할것임. 
            else
            {
                TEMP_Logger.Err($"Dest Pos doesnt have proper Occupier. This case is not handled yet....");
                return null;
            }
        }

        pathToDest = await pathBuffer.GetPath(from, to, moverFlag, modifier, ctk);

        if (ctk.IsCancellationRequested || pathToDest == null || pathToDest.Instance.Count == 0)
        {
            pathToDest?.ReturnToPool();

            return null;
        }

        return pathToDest;
    }
}
