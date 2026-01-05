using GameDB;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class MapManager : SingletonBase<MapManager>
{
    class LineCanPlaceCheckerCache
    {
        public E_EntityFlags MoverFlag;
        public Func<Vector2Int, bool> CanPlaceCheckerCache;
        public bool ResultCanPlace;
    }

    private Dictionary<string, MapData> _mapDataContainer;

    private MapNodeGrid _grid;

    private MeshRenderer _terrain;
    LineCanPlaceCheckerCache _walkableCheckerCache;

    public override void Initialize()
    {
        base.Initialize();

        _mapDataContainer = new Dictionary<string, MapData>();
        _walkableCheckerCache = new LineCanPlaceCheckerCache();
        _walkableCheckerCache.CanPlaceCheckerCache = (pos) =>
        {
            bool canPlace = _grid.CanPlace(pos.x, pos.y, _walkableCheckerCache.MoverFlag);
            if (canPlace == false)
                _walkableCheckerCache.ResultCanPlace = false;
            return canPlace;
        };
    }

    public MapData GetMapData(string key)
    {
        if (_mapDataContainer.ContainsKey(key) == false)
        {
            TEMP_Logger.Err($"Failed to get mapData : {key}");
            return null;
        }
        return _mapDataContainer[key].Copy(deepCopyObjects: true);
    }

    public void ApplyEntityToGrid(bool placedOrRemoved, EntityBase entity)
    {
        MapUtils.ApplyEntityFlagToGrid(_grid, placedOrRemoved, entity);
    }

    public bool IsInside(Vector3 pos)
    {
        return _grid.IsInside(pos);
    }

    public bool CanPlace(Vector2Int pos, E_EntityFlags moverFlag)
    {
        return _grid.CanPlace(pos.x, pos.y, moverFlag);
    }

    public bool CanPlace(Vector3 pos, E_EntityFlags moverFlag)
    {
        var tilePos = MapUtils.WorldPosToTilePos(pos);
        return _grid.CanPlace(tilePos.x, tilePos.y, moverFlag);
    }

    public bool CanPlaceFromTo(Vector3 from, Vector3 to, E_EntityFlags moverFlag)
    {
        _walkableCheckerCache.MoverFlag = moverFlag;
        _walkableCheckerCache.ResultCanPlace = true;

        MapUtils.ProcessCallBackTilesOnLine(
            MapUtils.WorldPosToTilePos(from),
            MapUtils.WorldPosToTilePos(to),
            _walkableCheckerCache.CanPlaceCheckerCache);

        return _walkableCheckerCache.ResultCanPlace;
    }

    public void AddMapData(MapData data)
    {
        if (_mapDataContainer.ContainsKey(data.name))
        {
            TEMP_Logger.Err($"Already Added MapData | Name : {data.name}");
            return;
        }

        _mapDataContainer.Add(data.name, data);
    }

    public async UniTask GenerateMap(string key)
    {
        var mapData = _mapDataContainer[key];

        if (mapData == null)
        {
            TEMP_Logger.Err($"Given mapData is NULL");
            return;
        }

        _grid = new MapNodeGrid();
        _grid.SetGrid(mapData.width, mapData.height);

        //MapUtils.ApplyEntityFlagToGrid(_grid, mapData.objects);

        //for (int i = 0; i < _grid.Count; i++)
        //{
        //    if (MapUtils.IsInsideArea(_grid[i].Position, mapData.activationLeftBottomMapPosition, mapData.activationRightTopMapPosition) == false)
        //    {
        //        _grid[i].ChangeFlag(E_TileStatusFlags.Unable);
        //    }
        //}

        PathFindingManager.Instance.PrepareGame(CameraManager.Instance.MainCam, _grid.MapNodes, mapData.width, mapData.height);

        _terrain = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshRenderer>();

        // 지금 이 평면 매터리얼같은 경우는 어쩔수없이 shadow 를 받아야 하기때문에
        // Receive Shadows 키워드를 써야만함. 이 때문에 기존 URP Shader 를 사용하는 부분에서
        // SRP Batch 가 깨질수밖에없는데, 문제는 애가 깨지면서 애 뒤로 잡혀있던 다른 정상적으로
        // 배치가 묶여야 하는 애들도 같이 깨져버려서 애 하나땜에 Batch 가 2가 늘어났었음.
        // 그래서 그 *뒤에 그려지던 애들* 이 앞에서 먼저 Batch 로 묶이게 하기 위해
        // 이 터레인을 2000 대보다 바로 뒤에 그려지게 해서 (Opaque RenderQueue)
        // 기존 SRP Batch 가 잘 작동하던 오브젝트들의 배치를 방해하지 않게 처리 참고.
        // (2450 부터 user defined 이라니까 2450 로 하자, 근데 이렇게 해도 깨지는 케이스가 있다.
        // 더 분석해봐야 할듯)
        _terrain.GetComponent<Renderer>().material.renderQueue = 2450;
        Destroy(_terrain.GetComponent<Collider>());

        var terrainMat = await AssetManager.Instance.LoadAsync<Material>(mapData.terrainMaterialKey);

        _terrain.material = terrainMat;

        _terrain.transform.localScale = new Vector3(mapData.width * 1.2f, mapData.height * 1.2f, 1);
        _terrain.transform.localRotation = Quaternion.Euler(90, 0, 0);
        _terrain.transform.position = new Vector3(mapData.width * 0.5f, 0, mapData.height * 0.5f);

        await EntityManager.Instance.PrepareGame(mapData.objects);
    }

    public Vector3 GetPoint(string key)
    {
        if (key == "PlayerModeEnterPosition")
            return new Vector3(39, 0, 50);

        TEMP_Logger.Err($"Failed to get Point | Key : {key}");

        return default;
    }

    public ulong GetTileOccupierID(Vector2Int pos)
    {
        return _grid[pos].OccupyingEntityID;
    }

    bool CheckCanPlace(Vector2Int position, E_EntityFlags flag)
    {
        return _grid.CanPlace(position.x, position.y, flag);
    }
}
