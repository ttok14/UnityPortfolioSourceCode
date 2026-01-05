using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

using Path = System.Collections.Generic.List<UnityEngine.Vector3>;
using System.Threading;
using System.Linq;
using GameDB;
using System.Text;

public class PathPoolInitData : IInstancePoolInitData
{
    public Path SourcePath;
    public Action<PathListPoolable> ReturnHandler;
}

public class PathListPoolable : PoolableList<Vector3>
{
    private Action<PathListPoolable> ReturnHandler;

    public override void OnPoolActivated(IInstancePoolInitData initData)
    {
        base.OnPoolActivated(initData);

        var data = initData as PathPoolInitData;

        Instance.Clear();

        if (data.SourcePath != null)
            Instance.AddRange(data.SourcePath);

        ReturnHandler = data.ReturnHandler;
    }

    public override void OnPoolReturned()
    {
        base.OnPoolReturned();

        ReturnHandler = null;
    }

    public override void ReturnToPool()
    {
        if (ReturnHandler != null)
        {
            ReturnHandler.Invoke(this);
        }
    }
}

public class PathBuffer
{
    public readonly struct Modifier
    {
        public readonly bool ApplyRandomizedDest;

        public readonly bool IsEnabled => ApplyRandomizedDest;

        public Modifier(bool applyRandomizedDest)
        {
            ApplyRandomizedDest = applyRandomizedDest;
        }
    }

    public readonly struct PathKey : IEquatable<PathKey>
    {
        public readonly Vector2Int From;
        public readonly Vector2Int To;
        public readonly E_EntityFlags MoverFlag;

        public PathKey(Vector2Int from, Vector2Int to, E_EntityFlags moverFlag)
        {
            From = from;
            To = to;
            MoverFlag = moverFlag;
        }
        public override string ToString()
        {
            return $"PathKeyInfo | From : {From} , To : {To}";
        }
        public bool Equals(PathKey other)
        {
            return From == other.From && To == other.To;
        }
        public override bool Equals(object obj)
        {
            return obj is PathKey other && Equals(other);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(From.GetHashCode(), To.GetHashCode());
        }
    }

    private Dictionary<PathKey, Path> _paths;

    // 비동기 작업에 대한 동시 요청 대응용 CompletionSource
    private Dictionary<PathKey, UniTaskCompletionSource<Path>> _completionSources;

    private ListInstancePool<PathListPoolable, Vector3> _pathInstancePool;
    private Action<PathListPoolable> _pathInstanceReturnHandler;
    private PathPoolInitData _pathInstanceInitData;

    // CancellationTokenSource _ctkSrc;

    public void Initialize()
    {
        _paths = new Dictionary<PathKey, Path>();
        _completionSources = new Dictionary<PathKey, UniTaskCompletionSource<Path>>();
        _pathInstancePool = new ListInstancePool<PathListPoolable, Vector3>();
        _pathInstanceReturnHandler = (poolableList) => _pathInstancePool.Return(poolableList);
        _pathInstanceInitData = new PathPoolInitData();

        // _ctkSrc = new CancellationTokenSource();
    }

    public void Release()
    {
        ClearAll();
    }

    public void ClearAll()
    {
        // CancelToken();
        // _ctkSrc = new CancellationTokenSource();

        _paths.Clear();

        // 이건 재사용하게 그냥 삭제안함. 대충보니까
        // 100 개언저리임. 감당가능.
        // _pathInstancePool.Clear();
    }

    HashSet<Vector2Int> _tilesOnPathCache = new HashSet<Vector2Int>();
    public List<PathKey> GetOverlappedPathKeys(IEnumerable<Vector2Int> checkTiles, List<PathKey> result)
    {
        result.Clear();

        foreach (var existingPath in _paths)
        {
            _tilesOnPathCache.Clear();
            MapUtils.GetTilesOnPath(existingPath.Value, _tilesOnPathCache);

            foreach (var tile in checkTiles)
            {
                if (_tilesOnPathCache.Contains(tile))
                {
                    result.Add(existingPath.Key);
                    break;
                }
            }
        }

        return result;
    }

    //void CancelToken()
    //{
    //    if (_ctkSrc != null)
    //    {
    //        _ctkSrc.Cancel();
    //        _ctkSrc.Dispose();
    //        _ctkSrc = null;
    //    }
    //}

    public void RegisterManually(Vector2Int from, Vector2Int to, E_EntityFlags moverFlag, Path path)
    {
        var pathKey = new PathKey(from, to, moverFlag);
        _paths[pathKey] = path;
    }

    public PathListPoolable TryGetPath(PathKey key, Modifier modifier)
    {
        if (_paths.TryGetValue(key, out var path) == false)
            return null;
        return AssignPathList(path, modifier);
    }

    public async UniTask<PathListPoolable> GetPath(Vector3 from, Vector3 to, E_EntityFlags moverFlag, Modifier modifier, CancellationToken ctk = default)
    {
        var res = await GetPathInternal(from, to, moverFlag, ctk);

        if (ctk.IsCancellationRequested || res == null)
        {
            return null;
        }

        return AssignPathList(res, modifier);
    }

    public PathListPoolable GetEmtpyPath()
    {
        _pathInstanceInitData.SourcePath = null;
        _pathInstanceInitData.ReturnHandler = _pathInstanceReturnHandler;

        return _pathInstancePool.GetOrCreate(_pathInstanceInitData);
    }

    public bool RemovePath(List<PathKey> pathKeys)
    {
        if (pathKeys == null)
            return false;

        bool hasRemoved = false;

        for (int i = 0; i < pathKeys.Count; i++)
        {
            if (_paths.Remove(pathKeys[i]))
                hasRemoved = true;
        }

        return hasRemoved;
    }

    //public async UniTask InvalidatePathAsync(PathKey pathKey, E_EntityFlags moverFlag, CancellationToken ctk)
    //{
    //    if (_paths.ContainsKey(pathKey) == false)
    //        return;

    //    _paths.Remove(pathKey);

    //    await GetPathInternal(MapUtils.TilePosToWorldPos(pathKey.From), MapUtils.TilePosToWorldPos(pathKey.To), moverFlag, ctk);
    //}

    public Path ApplyModifier(Path src, Modifier modifier)
    {
        if (modifier.ApplyRandomizedDest)
            src[src.Count - 1] = MapUtils.SomewhereInTilePos(src[src.Count - 1]);

        return src;
    }

    private async UniTask<Path> GetPathInternal(Vector3 from, Vector3 to, E_EntityFlags moverFlag, CancellationToken ctk)
    {
        var pathKey = new PathKey(MapUtils.WorldPosToTilePos(from), MapUtils.WorldPosToTilePos(to), moverFlag);

        if (_paths.TryGetValue(pathKey, out var path))
        {
            return path;
        }

        if (_completionSources.TryGetValue(pathKey, out var existingSrc))
        {
            try
            {
                // Cancel 된 경우 TrySetCanceled 을 호출하면
                // 이 Task 를 await 하는 쪽에서는 Exception 이 던져진다함
                // (TODO : 테스트)
                var res = await existingSrc.Task;
                if (ctk.IsCancellationRequested)
                {
                    return null;
                }
                return res;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception exp)
            {
                TEMP_Logger.Err(exp.Message);
                return null;
            }
        }

        var completionSrc = new UniTaskCompletionSource<Path>();

        _completionSources.Add(pathKey, completionSrc);

        try
        {
            var p = await FindPathAsync(from, to, moverFlag, completionSrc, ctk);

            if (ctk.IsCancellationRequested)
                return null;
            else if (p != null)
            {
                /// 참고로 이 시점에서 <see cref="RegisterManually(Vector2Int, Vector2Int, E_EntityFlags, Path)"/>
                /// 같은 동기 함수가 현 함수의 비동기 루틴 중간에 등록해버릴 수 있음.
                /// (현재 이슈는 없을듯)
                _paths[pathKey] = p;
            }

            return p;
        }
        catch (Exception ex)
        {
            completionSrc.TrySetException(ex);
            throw;
        }
        finally
        {
            _completionSources.Remove(pathKey);
        }
    }

    private async UniTask<Path> FindPathAsync(
        Vector3 from,
        Vector3 to,
        E_EntityFlags moverFlag,
        UniTaskCompletionSource<Path> completionSrc,
        CancellationToken ctk)
    {
        var path = new Path();

        await PathFindingManager.Instance.PathFinder.FindPath(
                from,
                to,
                path,
                moverFlag,
                ctk,
                new PathFinding.Config(trimPaths: true));

        if (ctk.IsCancellationRequested)
        {
            completionSrc.TrySetCanceled(ctk);
            return null;
        }

        if (path.Count == 0)
        {
#if UNITY_EDITOR
            // 왜 이동못하는 곳으로 PathFind 가 들어온거지
            var marker_from = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var marker_to = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker_from.transform.position = from;
            marker_to.transform.position = to;
            marker_from.name = $"ERROR_From_{from}";
            marker_to.name = $"ERROR_To_{to}";
#endif

            TEMP_Logger.Err($"Path Count Zero Why? | from: {from} , to : {to}");

            completionSrc.TrySetException(new Exception("Failed to Find Path!!"));
            return null;
        }

        completionSrc.TrySetResult(path);

        return path;
    }

    private PathListPoolable AssignPathList(Path source, Modifier modifier)
    {
        _pathInstanceInitData.SourcePath = source;
        _pathInstanceInitData.ReturnHandler = _pathInstanceReturnHandler;

        var instance = _pathInstancePool.GetOrCreate(_pathInstanceInitData);
        if (modifier.IsEnabled)
            ApplyModifier(instance.Instance, modifier);

        return instance;
    }
}
