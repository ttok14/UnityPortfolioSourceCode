using UnityEngine;

public class PathFindingManager : SingletonBase<PathFindingManager>
{
    public PathFinding PathFinder { get; private set; }

    public override void Initialize()
    {
        base.Initialize();
        PathFinder = new PathFinding();
    }

    public void PrepareGame(Camera cam, MapNode[] nodes, int width, int height)
    {
        // pathfinder 는 원본 node 에 대한 Refernrece 만 가지면서
        // 값이 바뀌는 것을 추적할 필요없이 최신 상태를 기준으로 계산 가능
        PathFinder.Initialize(cam, nodes, width, height);
    }

    public override void Release()
    {
        base.Release();

        PathFinder.Release();
    }
}
