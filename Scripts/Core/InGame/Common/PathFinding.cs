using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GameDB;
using Cysharp.Threading.Tasks;
using System.Threading;

public struct MapNode
{
    public readonly static MapNode INVALID = new MapNode(ushort.MaxValue, E_TileStatusFlags.None);

    // public Vector2Int Position;
    public ushort Position;
    //public Vector3 WorldPosition { get; private set; }

    public E_TileStatusFlags StatusFlag;

    public ulong OccupyingEntityID;

    public uint LastExecutionID;

    #region ===:: PathFinder 관련 요소 ::===
    public int PrevIdx;
    public int G;
    public int H;
    public int F => G + H;
    #endregion

    public MapNode(
        ushort position,
        E_TileStatusFlags statusFlags)
    {
        StatusFlag = statusFlags;
        Position = position;

        OccupyingEntityID = 0;
        PrevIdx = -1;
        G = 0;
        H = 0;

        LastExecutionID = 0;
    }

    public readonly static Vector3[] LocalOffsets = new Vector3[]
    {
        new Vector3(-Constants.MapNodeCellHalfSize,0,Constants.MapNodeCellHalfSize),
        new Vector3(0,0,Constants.MapNodeCellHalfSize),
        new Vector3(Constants.MapNodeCellHalfSize,0,Constants.MapNodeCellHalfSize),

        new Vector3(-Constants.MapNodeCellHalfSize,0,0),
        new Vector3(0,0,0),
        new Vector3(Constants.MapNodeCellHalfSize,0,0),

        new Vector3(-Constants.MapNodeCellHalfSize,0,-Constants.MapNodeCellHalfSize),
        new Vector3(0,0,-Constants.MapNodeCellHalfSize),
        new Vector3(Constants.MapNodeCellHalfSize,0,-Constants.MapNodeCellHalfSize)
    };

    //public void ResetCost()
    //{
    //    PrevIdx = -1;
    //    G = 0;
    //    H = 0;
    //}

    public void ChangeFlag(E_TileStatusFlags newFlag)
    {
        StatusFlag = newFlag;
    }

    public void Set(int g, int h, int prevIdx)
    {
        this.G = g;
        this.H = h;
        this.PrevIdx = prevIdx;
    }

    public void Set(int g, int h, int prevIdx, uint executionId)
    {
        this.G = g;
        this.H = h;
        this.PrevIdx = prevIdx;
        this.LastExecutionID = executionId;
    }
}

public class PathFinding
{
    struct NeighborInfo
    {
        public int Idx;
        public int MoveCost;
        public Vector2Int TilePosition;

        public NeighborInfo(int idx, int cost, Vector2Int tilePosition)
        {
            Idx = idx;
            MoveCost = cost;
            TilePosition = tilePosition;
        }
    }

    public struct Config
    {
        public bool trimPaths;

        public Config(bool trimPaths)
        {
            this.trimPaths = trimPaths;
        }

        //public float smoothnessLevel;

        //public Config(float smoothnessLevel)
        //{
        //    this.smoothnessLevel = smoothnessLevel;
        //}
    }

    const int LineNodeCost = 10;
    const int DiagonalNodeCost = 14;

    Camera _cam;

    int _width;
    int _height;

    MapNode[] _nodes;

    // 가비지 생성 방지위함. Clear 시에도 내부배열은 유지
    static PriorityQueue<int> OpenListCache = new PriorityQueue<int>();
    static HashSet<int> ClosedListCache = new HashSet<int>();
    static List<NeighborInfo> NeighborsListCache = new List<NeighborInfo>();

    Func<bool> _isProcessingWaitPassChecker;
    public void Initialize(Camera camera, MapNode[] nodes, int width, int height)
    {
        _cam = camera;
        _nodes = nodes;
        _width = width;
        _height = height;

        _isProcessingWaitPassChecker = delegate ()
        {
            return _isProcessing == false;
        };
    }

    public void Release()
    {
        _nodes = null;
    }

    //public MapNode GetNodeByScreenPoint(Vector3 screenPos)
    //{
    //    var ray = _cam.ScreenPointToRay(screenPos);
    //    var plane = new Plane(Vector3.up, Vector3.zero);
    //    if (plane.Raycast(ray, out var enter))
    //    {
    //        Vector3 intersectionPoint = ray.GetPoint(enter);
    //        Vector2Int nodeSpacePosition = new Vector2Int(Mathf.FloorToInt(intersectionPoint.x), Mathf.FloorToInt(intersectionPoint.z));

    //        if (IsInsideMap(nodeSpacePosition.x, nodeSpacePosition.y) == false)
    //        {
    //            return MapNode.INVALID;
    //        }

    //        return _nodes[nodeSpacePosition.x, nodeSpacePosition.y];
    //    }
    //    else
    //    {
    //        return null;
    //    }
    //}

    public Vector2Int IdxToTilePos(int idx)
    {
        return MapUtils.IdxToTilePos(idx, _width);
    }

    public Vector3 IdxToWorldPos(int idx)
    {
        return MapUtils.IdxToWorldPos(idx, _width);
    }

    public int WorldPosToIdx(Vector3 pos)
    {
        return MapUtils.WorldPosToIdx(pos, _width);
    }

    public int TilePosToIdx(Vector2Int pos)
    {
        return MapUtils.TilePosToIdx(pos, _width);
    }

    readonly static (int x, int y, int cost)[] _neighborOffsets = new (int x, int y, int cost)[]
    {
        (-1, 1, DiagonalNodeCost), (0, 1, LineNodeCost), (1, 1, DiagonalNodeCost),
        (-1, 0, LineNodeCost),    /* 중앙 패스 */         (1, 0, LineNodeCost),
        (-1, -1, DiagonalNodeCost), (0, -1, LineNodeCost), (1, -1, DiagonalNodeCost)
    };

    List<NeighborInfo> GetNeighbors(int origNodeIdx, HashSet<int> closedList, E_EntityFlags walkerFlag)
    {
        NeighborsListCache.Clear();

        var oriNode = _nodes[origNodeIdx];
        var oriPos = IdxToTilePos(oriNode.Position);
        // var startPos = IdxToTilePos(oriNode.Position) - new Vector2Int(-1, -1);

        for (int i = 0; i < _neighborOffsets.Length; i++)
        {
            var offset = _neighborOffsets[i];
            Vector2Int pos = new Vector2Int(oriPos.x + offset.x, oriPos.y + offset.y);

            if (IsInsideMap(pos) == false)
                continue;

            int idx = TilePosToIdx(pos);

            if (closedList.Contains(idx))
                continue;

            if (MapUtils.CanPlace(_nodes[idx].StatusFlag, walkerFlag) == false)
                continue;

            NeighborsListCache.Add(new NeighborInfo(idx, offset.cost, pos));
        }

        return NeighborsListCache;
    }

    bool IsInsideMap(int x, int z)
    {
        return x >= 0 && z >= 0 && x < _width && z < _height;
    }

    bool IsInsideMap(Vector2Int pos)
    {
        return pos.x >= 0 && pos.y >= 0 && pos.x < _width && pos.y < _height;
    }

    bool _isProcessing;
    int _waitingCount;
    static uint _currentExecutionID;

    public async UniTask<bool> FindPath(
        Vector3 startWorldPos,
        Vector3 destWorldPos,
        List<Vector3> result,
        E_EntityFlags walkerFlag,
        CancellationToken ctk = default,
        Config config = default)
    {
        if (_isProcessing)
        {
            _waitingCount++;

            try
            {
                await UniTask.WaitUntil(_isProcessingWaitPassChecker, cancellationToken: ctk);
            }
            catch (OperationCanceledException)
            {
                _waitingCount--;
                _isProcessing = false;
                return false;
            }

            _waitingCount--;
        }

        _isProcessing = true;
        _currentExecutionID++;

        result.Clear();

        // 너무 가까움 
        if (Vector3.SqrMagnitude(destWorldPos - startWorldPos) <= 0.2f)
        {
            _isProcessing = false;
            result.Add(startWorldPos);
            return true;
        }

        OpenListCache.Clear();
        ClosedListCache.Clear();

        Vector2Int startTilePos = MapUtils.WorldPosToTilePos(startWorldPos);
        Vector2Int destTilePos = MapUtils.WorldPosToTilePos(destWorldPos);

        int startIdx = TilePosToIdx(startTilePos);
        int destIdx = TilePosToIdx(destTilePos);

        // 최대한 가까운 위치로라도 잡아줘야할지는. 추후 고민 .. 
        if (IsInsideMap(destTilePos.x, destTilePos.y) == false)
        {
            _isProcessing = false;
            return false;
        }

        var destNode = _nodes[destIdx];

        // 최대한 가까운 위치로라도 잡아줘야할지는. 추후 고민 .. 
        if (MapUtils.CanPlace(destNode.StatusFlag, walkerFlag) == false)
        {
            _isProcessing = false;
            return false;
        }

        //for (int i = 0; i < _nodes.Length; i++)
        //{
        //    _nodes[i].ResetCost();
        //}

        _nodes[startIdx].Set(
            0,
            Mathf.Max(Mathf.Abs(destTilePos.x - startTilePos.x), Mathf.Abs(destTilePos.y - startTilePos.y)) * LineNodeCost,
            -1,
            _currentExecutionID);

        var startNode = _nodes[startIdx];

        result.Clear();

        OpenListCache.Enqueue(startIdx, startNode.F);

        ulong frameCountToTakeBreak = 400;
        ulong tryCount = 0;

        while (OpenListCache.Count > 0)
        {
            tryCount++;

            // PathFinding 대기중인 작업이 있다면
            // 지체없이 바로 끝내야함.. 
            if (_waitingCount == 0 && tryCount >= frameCountToTakeBreak)
            {
                tryCount = 0;

                await UniTask.Yield();

                if (ctk.IsCancellationRequested)
                {
                    _isProcessing = false;
                    return false;
                }
            }

            var currentIdx = OpenListCache.Dequeue();
            var currentNode = _nodes[currentIdx];

            if (currentNode.H <= 1)
            {
                // result.Add(MapUtils.TilePosToWorldPos(destNode.Position));
                result.Add(destWorldPos);
                result.Add(IdxToWorldPos(currentNode.Position));

                int prevIdx = currentNode.PrevIdx;

                while (prevIdx != -1)
                {
                    var pnode = _nodes[prevIdx];
                    result.Add(IdxToWorldPos(pnode.Position));
                    prevIdx = pnode.PrevIdx;
                }

                // TODO : 이거 어떻게 수정안되나 , 읽는쪽에서 역으로 읽으라는
                // 컨벤션을 두는건 좀 아닌거같고 ..
                result.Reverse();

                if (config.trimPaths)
                    TrimPaths(result, walkerFlag);

                _isProcessing = false;

                return true;
            }

            ClosedListCache.Add(currentIdx);
            var neighbors = GetNeighbors(currentIdx, ClosedListCache, walkerFlag);

            foreach (var nb in neighbors)
            {
                int newGcost = currentNode.G + nb.MoveCost;

                // bool isNewNode = nbNode.G == 0;
                bool isNewNode = _nodes[nb.Idx].LastExecutionID != _currentExecutionID;

                // 방문하지 않은 노드 (G 코스트 == 0)
                if (isNewNode)
                {
                    int H = Mathf.Max(Mathf.Abs(destTilePos.x - nb.TilePosition.x), Mathf.Abs(destTilePos.y - nb.TilePosition.y)) * LineNodeCost;

                    _nodes[nb.Idx].Set(
                        newGcost,
                        H,
                        currentIdx,
                        _currentExecutionID);

                    OpenListCache.Enqueue(nb.Idx, newGcost + H);
                }
                // 이미 방문한 노드 
                else
                {
                    // 현재 탐색중인 새로운 G 코스트가 기존에 이미 방문한 노드의 G 코스트보다
                    // 더 낮다면 (저렴하면) 더 효율적인 경로를 발견한 것. 갱신 .
                    if (newGcost < _nodes[nb.Idx].G)
                    {
                        _nodes[nb.Idx].Set(
                            newGcost,
                            _nodes[nb.Idx].H,
                            currentIdx);

                        OpenListCache.Enqueue(nb.Idx, _nodes[nb.Idx].F);
                    }
                }
            }
        }

        _isProcessing = false;

        return true;
    }

    public void TrimPaths(List<Vector3> paths, E_EntityFlags walkerFlag)
    {
        // 마지막 검출된 타일 위치 => DestPos 는 거리가 짧을 확률이 크기땜에 끊기는 느낌 방지 하기 위해
        // 바로 dest 로 이동 처리
        if (paths.Count > 2)
        {
            // 실제 길이 1 을 체크하기 위해 , 1^2 은 1이기에 그냥 매직넘버로 씀 
            if (Vector3.SqrMagnitude(paths[paths.Count - 1] - paths[paths.Count - 2]) < 1f)
            {
                paths.RemoveAt(paths.Count - 2);
            }
        }

        int smoothFromIdx = 0;

        for (int i = 0; i < paths.Count; i++)
        {
            if (i + 2 >= paths.Count)
            {
                break;
            }

            var start = paths[i];
            var middle = paths[i + 1];
            var end = paths[i + 2];

            var dirToMiddle = (middle - start).normalized;
            var dirToEnd = (end - middle).normalized;
            float curveAng = Vector3.Angle(dirToMiddle, dirToEnd);

            // 이동 경로가 거의 직선에 가깝다면 중간 포인트들 Trim 처리
            if (curveAng < 5)
            {
                paths.RemoveAt(i + 1);
                i = i - 1;
                continue;
            }
            // 터닝을 어느정도 해줘야 하는 경로라면
            // 중간 경로들의 isWalkable 체크를 해준 다음
            // 쳐내도 되면 쳐내버림 (위에서 직선에 가까운 포인트들은 이미 중간에 쳐내는 처리를
            // 하기 때문에 여기서 포인트와 포인트 사이 거리가 길거임)
            else if (curveAng >= 15)
            {
                var smoothFrom = paths[smoothFromIdx];
                int smoothToIdx = i + 2;
                var smoothTo = paths[smoothToIdx];

                // 잇기 위한(smooth 를 위한) 포인트 시작과 끝의 거리를 잰 다음
                // 사이사이의 노드들의 isWalkalbe 을 체크하기 위해 .
                // ?? 근데 0 이 되면 우짜까
                /// TODO : 브레젠험 알고리즘으로 대체할까 ?
                int lerpStepCount = (int)Vector3.Distance(smoothFrom, smoothTo);

                for (int j = 0; j < lerpStepCount; j++)
                {
                    // 사이에 단계별로 존재하는 타일들의 isWalkable 을 체크하기 위함
                    // var toTilePos = MapUtils.WorldPosToTilePos(Vector3.Lerp(smoothFrom, smoothTo, j / (float)lerpStepCount));
                    var pos = Vector3.Lerp(smoothFrom, smoothTo, j / (float)lerpStepCount);
                    bool isWalkable = MapUtils.CanPlace(_nodes[WorldPosToIdx(pos)].StatusFlag, walkerFlag);

                    if (isWalkable)
                    {
                        if (j == lerpStepCount - 1)
                        {
                            paths.RemoveRange(smoothFromIdx + 1, smoothToIdx - smoothFromIdx - 1);
                            i = smoothToIdx;
                            smoothFromIdx = smoothToIdx;
                            break;
                        }
                    }
                    // walkable 하지않은게 중간에 하나라도 있다면
                    // 현재 세점은 무조건 이동하기로한다. 
                    else
                    {
                        smoothFromIdx = smoothToIdx;
                        break;
                    }
                }
            }
        }
    }

    public class PriorityQueue<T>
    {
        private List<(T item, int priority)> _heap = new List<(T, int)>();

        public int Count => _heap.Count;
        public void Clear() => _heap.Clear();

        public void Enqueue(T item, int priority)
        {
            _heap.Add((item, priority));
            SiftUp(_heap.Count - 1);
        }

        public T Dequeue()
        {
            if (_heap.Count == 0) throw new InvalidOperationException("Empty queue");
            var root = _heap[0].item;
            _heap[0] = _heap[_heap.Count - 1];
            _heap.RemoveAt(_heap.Count - 1);
            if (_heap.Count > 0) SiftDown(0);
            return root;
        }

        private void SiftUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_heap[index].priority < _heap[parent].priority)
                {
                    Swap(index, parent);
                    index = parent;
                }
                else break;
            }
        }

        private void SiftDown(int index)
        {
            int count = _heap.Count;
            while (true)
            {
                int left = index * 2 + 1;
                int right = index * 2 + 2;
                int smallest = index;

                if (left < count && _heap[left].priority < _heap[smallest].priority)
                    smallest = left;
                if (right < count && _heap[right].priority < _heap[smallest].priority)
                    smallest = right;

                if (smallest != index)
                {
                    Swap(index, smallest);
                    index = smallest;
                }
                else break;
            }
        }

        private void Swap(int i, int j)
        {
            var tmp = _heap[i];
            _heap[i] = _heap[j];
            _heap[j] = tmp;
        }
    }
}
