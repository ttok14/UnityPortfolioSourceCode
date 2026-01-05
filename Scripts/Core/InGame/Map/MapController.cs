//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using GameDB;

//public class MapController : PoolableObjectBase
//{
//    private MapNodeGrid _grid;
//    public PathFinding PathFinder { get; private set; }

//    private Dictionary<ulong, EntityBase> _mapEntities;
//    private Transform _entityRoot;
//    private Transform[] _entityRootsByType;

//    private MeshRenderer _terrain;

//    // 맵
//    public override void OnSpawned(ObjectPoolCategory category, string key)
//    {
//        base.OnSpawned(category, key);
//        _grid = new MapNodeGrid();
//        PathFinder = new PathFinding();

//        //_entityRoot = new GameObject("EntityRoot").transform;

//        //var entityTypes = (E_EntityType[])System.Enum.GetValues(typeof(E_EntityType));
//        //_entityRootsByType = new Transform[entityTypes.Length];
//        //for (int i = 0; i < entityTypes.Length; i++)
//        //{
//        //    _entityRootsByType[i] = new GameObject(entityTypes[i].ToString()).transform;
//        //    _entityRootsByType[i].SetParent(_entityRoot);
//        //}
//    }

//    public IEnumerator Generate(MapData mapData)
//    {
//        if (mapData == null)
//        {
//            TEMP_Logger.Err($"Given mapData is NULL");
//            yield break;
//        }

//        _grid.SetGrid(mapData.width, mapData.height);
//        MapUtils.ApplyEntityFlagToGrid(_grid, mapData.objects);
//        PathFinder.Initialize(CameraManager.Instance.MainCam, _grid.MapNodes);

//        bool entitiesGeneratedFinished = false;

//        _mapEntities = new Dictionary<ulong, EntityBase>(mapData.objects.Count);

//        foreach (var data in mapData.objects)
//        {
//            var tableData = DBEntity.Get(data.entityId);
//            if (tableData == null)
//            {
//                TEMP_Logger.Err($"Failed to get Entity Table Data | ID : {data.entityId}");
//                continue;
//            }

//            // TODO : 오너는 시스템을 조금더 기획하고 수정하자.
//            EntityFactory.CreateEntity(data, EntityOwnerType.Player, _entityRootsByType[(int)tableData.EntityType],
//                onCompleted: (created) =>
//                 {
//                     _mapEntities.Add(created.ID, created);

//                     if (_mapEntities.Count == mapData.objects.Count)
//                     {
//                         entitiesGeneratedFinished = true;
//                     }
//                 });
//        }

//        _terrain = GameObject.CreatePrimitive(PrimitiveType.Plane).GetComponent<MeshRenderer>();
//        Destroy(_terrain.GetComponent<Collider>());
//        yield return ResourceManager.Instance.LoadCo<Material>(mapData.terrainMaterialKey, (res) =>
//        {
//            _terrain.material = res;

//            _terrain.transform.localScale = new Vector3(mapData.width * 0.12f, 1f, mapData.height * 0.12f);
//            _terrain.transform.position = new Vector3(mapData.width * 0.5f, 0, mapData.height * 0.5f);
//        });

//        yield return new WaitUntil(() => entitiesGeneratedFinished);
//    }

//    public void Release()
//    {

//    }

//    public bool ShowGizmos;

//    private void OnDrawGizmos()
//    {
//        if (Application.isPlaying == false || ShowGizmos == false)
//        {
//            return;
//        }

//        for (int x = 0; x < _grid.Width; x++)
//        {
//            for (int z = 0; z < _grid.Height; z++)
//            {
//                Vector2Int pos = new Vector2Int(x, z);

//#if UNITY_EDITOR


//                if (_grid[x, z].StatusFlag != E_TileStatusFlags.Standard_Terrain)
//                {
//                    string text = $"";
//                    var ori = Gizmos.color;
//                    Gizmos.color = Color.red;
//                    Gizmos.DrawCube(MapUtils.TilePosToWorldPos(new Vector2Int(x, z)), Vector3.one);
//                    Gizmos.color = ori;
//                    UnityEditor.Handles.Label(MapUtils.TilePosToWorldPos(new Vector2Int(x, z)) + Vector3.up * 0.5f, $"Pos:{x},{z}");
//                }
//#endif
//            }
//        }
//    }
//}
