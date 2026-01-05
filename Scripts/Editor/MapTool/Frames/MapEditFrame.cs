using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace Tool
{
    public class MapEditFrame : MapToolFrameBase<MapToolWindow>
    {
        static Plane _floorPlane = new Plane(Vector3.up, Vector3.zero);
        bool _pickingIntersected;
        Vector3 _pickingIntersectionPoint;
        Vector2Int _pickingTilePosition;

        GridDrawer _grid = new GridDrawer();

        Dictionary<string, CustomAddressableAssetEntry> _mapObjecAddressableEntries;
        Dictionary<ulong, EntityObjectEditor> _sceneMapObjects = new Dictionary<ulong, EntityObjectEditor>();
        GameObject _dynamicGeneratedRoot;

        bool _isEntityModified;

        MeshRenderer _terrainRenderer;

        GameObject _enemySpawnPointIndicatorPrefab;
        Dictionary<Vector2Int, GameObject> _enemySpawnPointIndicators;
        bool _isEnemySpawnPointDirty;

        bool _isBoundaryRecording;
        bool _drawBoundary;
        bool _isLeftMouseDown;
        bool _isRightMouseDown;
        List<Vector2Int> _boundaryRecordPositions = new List<Vector2Int>();

        bool _drawPathfindingInactivatedArea;
        bool _pathfindingActivationDataDirty;

        bool _showTileOpMenu = true;

        List<EntityObjectEditor> _selectedObjects = new List<EntityObjectEditor>();

        public MapEditFrame(MapToolWindow parent) : base(parent)
        {

        }

        public override void OnEnable()
        {
            base.OnEnable();
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _mapObjecAddressableEntries = MapToolUtil.IterateGroupAssets("Entity", typeof(GameObject)).ToDictionary(
                keySelector: (t) => t.address,
                elementSelector: (t) => t);
            _enemySpawnPointIndicatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameAssets/Addressables_Remote/FX/Prefab/EnemySpawnPoint.prefab");
            LoadNew();
        }

        public override void OnMapLoadedNew(IEnumerable<EntityObjectData> list)
        {
            base.OnMapLoadedNew(list);
            LoadNew();
        }

        void LoadNew()
        {
            ClearObjects();
            CreateTerrain();

            _grid.Change(Owner.WorkingMapData.width, Owner.WorkingMapData.height);
            foreach (var data in Owner.WorkingMapData.objects)
            {
                AddObject(data, false);
            }

            _enemySpawnPointIndicators = new Dictionary<Vector2Int, GameObject>();

            foreach (var pos in Owner.WorkingMapData.enemySpawnPositions)
            {
                var indicator = GameObject.Instantiate(_enemySpawnPointIndicatorPrefab);
                indicator.transform.position = _grid.TilePosToWorldPos(pos);
                _enemySpawnPointIndicators.Add(pos, indicator);
            }
        }

        private void CreateTerrain()
        {
            if (_terrainRenderer)
            {
                GameObject.DestroyImmediate(_terrainRenderer.gameObject);
            }

            var terrainGroupMats = MapToolUtil.IterateGroupAssets("Terrain", typeof(Material));
            var mat = terrainGroupMats.Find(t => t.address == Owner.WorkingMapData.terrainMaterialKey);
            if (mat == null)
            {
                Debug.LogError($"Could not find the terrina mateiral | Key : {Owner.WorkingMapData.terrainMaterialKey}");
                return;
            }

            _terrainRenderer = GameObject.CreatePrimitive(PrimitiveType.Plane).GetComponent<MeshRenderer>();
            _terrainRenderer.transform.localScale = new Vector3(Owner.WorkingMapData.width * 0.12f, 1, Owner.WorkingMapData.height * 0.12f);
            _terrainRenderer.transform.localPosition = new Vector3(Owner.WorkingMapData.width * 0.5f, 0, Owner.WorkingMapData.height * 0.5f);
            _terrainRenderer.sharedMaterial = new Material(mat.mainAsset as Material);
            GameObject.DestroyImmediate(_terrainRenderer.GetComponent<MeshCollider>());
        }

        EntityObjectEditor AddObject(EntityObjectData data, bool addedDirty)
        {
            if (!_dynamicGeneratedRoot)
            {
                _dynamicGeneratedRoot = new GameObject();
                _dynamicGeneratedRoot.name = "ObjectRoot";
            }

            var tableData = Owner.TableContainer.Container.EntityTable_data[data.entityId];
            var key = tableData.ResourceKey;
            var asset = _mapObjecAddressableEntries[key].mainAsset as GameObject;
            var created = GameObject.Instantiate<GameObject>(asset, _dynamicGeneratedRoot.transform);
            created.transform.position = data.worldPosition;
            created.transform.eulerAngles = new Vector3(0, data.eulerRotY, 0);
            var comp = created.AddComponent<EntityObjectEditor>();
            ulong instanceId = EntityObjectEditor.NextInstanceID;
            comp.Setup(addedDirty, data.entityId, Owner.TableContainer.Container.EntityTable_data[data.entityId], instanceId, data.teamType, _mapObjecAddressableEntries[key]);

            _sceneMapObjects.Add(instanceId, comp);

            return comp;
        }

        void RemoveObject(EntityObjectEditor comp)
        {
            _sceneMapObjects.Remove(comp.InstanceID);
            if (_selectedObjects.Contains(comp))
                _selectedObjects.Remove(comp);
            GameObject.DestroyImmediate(comp.gameObject);
        }

        void ClearTerrain()
        {
            if (_terrainRenderer)
            {
                GameObject.DestroyImmediate(_terrainRenderer.gameObject);
                _terrainRenderer = null;
            }
        }

        void ClearEnemySpawnPoints()
        {
            if (_enemySpawnPointIndicators != null)
            {
                foreach (var indicator in _enemySpawnPointIndicators)
                {
                    GameObject.DestroyImmediate(indicator.Value.gameObject);
                }

                _enemySpawnPointIndicators.Clear();
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            ClearObjects();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            ClearObjects();
        }

        void ClearObjects()
        {
            if (_dynamicGeneratedRoot)
            {
                GameObject.DestroyImmediate(_dynamicGeneratedRoot);
                _sceneMapObjects.Clear();
                _dynamicGeneratedRoot = null;
            }

            ClearTerrain();
            ClearEnemySpawnPoints();
        }

        public override void OnBeforeAssemblyReload()
        {
            base.OnBeforeAssemblyReload();

            ClearObjects();
        }

        public override void OnAfterAssemblyReload()
        {
            base.OnAfterAssemblyReload();
        }

        public override void OnGUI()
        {
            base.OnGUI();

            if (Owner.WorkingMapData == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical(GUI.skin.window);
            {
                if (GUILayout.Button($"임시 작업 ㄱㄱ {_sceneMapObjects.Count}"))
                {
                    foreach (var obj in _sceneMapObjects)
                    {
                        _isEntityModified = true;

                        var t = Owner.TableContainer.Container.EntityTable_data[obj.Value.TableID];

                        if (t.EntityType == GameDB.E_EntityType.Structure &&
                            Owner.TableContainer.Container.StructureTable_data.ContainsKey(t.DetailTableID))
                        {
                            if (t.ID == 167 || t.ID == 168 || t.ID == 169)
                                obj.Value.TeamType = EntityTeamType.Enemy;
                            else
                            {
                                obj.Value.TeamType = EntityTeamType.Player;
                                Debug.LogError($"내 팀 : {obj.Key} ");
                            }
                        }
                        else
                        {
                            obj.Value.TeamType = EntityTeamType.Neutral;
                        }
                    }
                }

                if (GUILayout.Button("맵 오브젝트 추가"))
                {
                    Vector2 mousePos = Event.current.mousePosition;
                    Rect popupRect = new Rect(
                        mousePos,
                        Vector2.zero
                    );

                    PopupWindow.Show(popupRect, new AddEntityContentPopup(
                        Owner.TableContainer.Container.EntityTable_data,
                        (resId, resPos, resEulerY, team) =>
                        {
                            var data = new EntityObjectData(resPos, resEulerY, resId, team);
                            AddObject(data, true);
                        }));
                }

                var objs = Selection.gameObjects;
                bool activeRemoveBtn = false;

                if (objs != null)
                {
                    _selectedObjects.Clear();

                    foreach (var obj in objs)
                    {
                        var comp = obj.GetComponent<EntityObjectEditor>();
                        if (comp == null)
                            continue;

                        if (_selectedObjects.Contains(comp) == false)
                        {
                            _selectedObjects.Add(comp);
                        }
                    }
                }

                if (_selectedObjects.Count > 0)
                    activeRemoveBtn = true;

                bool prevEnabled = GUI.enabled;
                GUI.enabled = activeRemoveBtn;
                if (GUILayout.Button($"맵 오브젝트 삭제 ({_selectedObjects.Count}개)"))
                {
                    if (EditorUtility.DisplayDialog("확인", "정말로 삭제하시겠습니까?", "삭제", "취소"))
                    {
                        _isEntityModified = true;

                        int cnt = _selectedObjects.Count;
                        for (int i = 0; i < cnt; i++)
                        {
                            RemoveObject(_selectedObjects[0]);
                        }

                        Owner.Repaint();
                    }

                }
                GUI.enabled = prevEnabled;

                EditorGUI.BeginChangeCheck();
                {
                    Owner.WorkingMapData.activationLeftBottomMapPosition = EditorGUILayout.Vector2IntField("길찾기 활성화 LeftBottom 위치 (최적화)", Owner.WorkingMapData.activationLeftBottomMapPosition);
                    Owner.WorkingMapData.activationRightTopMapPosition = EditorGUILayout.Vector2IntField("길찾기 활성화 RightTop 위치 (최적화)", Owner.WorkingMapData.activationRightTopMapPosition);
                }
                if (EditorGUI.EndChangeCheck())
                    _pathfindingActivationDataDirty = true;

                _drawPathfindingInactivatedArea = EditorGUILayout.Toggle("길찾기 비활성화 보이기", _drawPathfindingInactivatedArea);

                EditorGUILayout.Space();

                // -------------------------------------------------//


                _drawBoundary = EditorGUILayout.Toggle("바운더리 보이기", _drawBoundary);

                _isBoundaryRecording = EditorGUILayout.Toggle("바운더리 녹화", _isBoundaryRecording);

                if (GUILayout.Button("바운더리 제거"))
                {
                    _boundaryRecordPositions.Clear();
                }
                EditorGUILayout.SelectableLabel(string.Join(",", _boundaryRecordPositions));
                if (GUILayout.Button("바운더리 클립보드 복사"))
                {
                    EditorGUIUtility.systemCopyBuffer = string.Join(",", _boundaryRecordPositions).Replace(" ", "");
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawCreateGUI()
        {
            EditorGUILayout.BeginVertical();
            {
                // _createSize = EditorGUILayout.Vector2IntField("맵 사이즈", _createSize);

            }
            EditorGUILayout.EndVertical();
        }

        private void DrawEditGUI()
        {
        }
        public override void OnMapSizeChanged(Vector2Int newSize)
        {
            base.OnMapSizeChanged(newSize);
            _grid.Change(newSize.x, newSize.y);
            RefreshTerrain();
        }

        public override void OnTerrainMaterialChanged(string key)
        {
            base.OnTerrainMaterialChanged(key);

            RefreshTerrain();
        }

        public override void OnMapObjectAdded(IEnumerable<EntityObjectData> added)
        {
            base.OnMapObjectAdded(added);


        }

        public override void OnMapObjectCleared()
        {
            base.OnMapObjectCleared();

            ClearObjects();
        }

        public override void OnMapObjectRemoved(IEnumerable<EntityObjectData> removed)
        {
            base.OnMapObjectRemoved(removed);
        }

        public override void DrawSceneGUI(SceneView sv)
        {
            base.DrawSceneGUI(sv);

            DrawPickingPosition(sv);
            DrawEnemySpawnPoints(sv);
            DrawTileOperationMenu(sv);
        }


        private void DrawTileOperationMenu(SceneView sv)
        {
            var evt = Event.current;

            if (evt.type == EventType.MouseDown && evt.button == 1)
            {
                _showTileOpMenu = true;
            }
            else if (evt.type == EventType.MouseDrag)
            {
                _showTileOpMenu = false;
            }
            else if (evt.type == EventType.MouseUp && evt.button == 1)
            {
                if (_showTileOpMenu == false)
                {
                    _showTileOpMenu = true;
                    return;
                }

                GenericMenu menu = new GenericMenu();

                if (Owner.WorkingMapData.enemySpawnPositions.Contains(_pickingTilePosition) == false)
                {
                    menu.AddItem(new GUIContent($"적 스폰 위치로 설정 ({_pickingTilePosition})"), false, () =>
                    {
                        if (Owner.WorkingMapData.enemySpawnPositions == null)
                        {
                            Owner.WorkingMapData.enemySpawnPositions = new List<Vector2Int>();
                        }

                        if (Owner.WorkingMapData.enemySpawnPositions.Contains(_pickingTilePosition))
                        {
                            EditorUtility.DisplayDialog("오류", "이미 존재함", "확인");
                        }
                        else
                        {
                            var indicator = GameObject.Instantiate(_enemySpawnPointIndicatorPrefab);
                            indicator.transform.position = _grid.TilePosToWorldPos(_pickingTilePosition);
                            _enemySpawnPointIndicators.Add(_pickingTilePosition, indicator);

                            Owner.WorkingMapData.enemySpawnPositions.Add(_pickingTilePosition);

                            _isEnemySpawnPointDirty = true;
                        }
                    });
                }

                if (Owner.WorkingMapData.enemySpawnPositions.Contains(_pickingTilePosition))
                {
                    menu.AddItem(new GUIContent($"적 스폰 위치 삭제 ({_pickingTilePosition})"), false, () =>
                    {
                        GameObject.DestroyImmediate(_enemySpawnPointIndicators[_pickingTilePosition].gameObject);
                        _enemySpawnPointIndicators.Remove(_pickingTilePosition);
                        Owner.WorkingMapData.enemySpawnPositions.Remove(_pickingTilePosition);

                        _isEnemySpawnPointDirty = true;
                    });
                }

                menu.DropDown(new Rect(evt.mousePosition, Vector2.zero));
                evt.Use();
            }
        }

        void DrawPickingPosition(SceneView sv)
        {
            var e = Event.current;

            if (_pickingIntersected)
            {
                var style = new GUIStyle(GUI.skin.label);
                style.normal.textColor = Color.white;
                style.fontSize = 14;
                style.fontStyle = FontStyle.Bold;
                var content = new GUIContent(_pickingIntersectionPoint.ToString());

                GUI.Label(new Rect(e.mousePosition.x + 10, e.mousePosition.y + 15, style.CalcSize(content).x + 5, 30), content, style);
            }
        }

        void DrawEnemySpawnPoints(SceneView sv)
        {
            var e = Event.current;

            foreach (var indicator in _enemySpawnPointIndicators)
            {
                var pos = _grid.TilePosToWorldPos(indicator.Key);
                var style = new GUIStyle(GUI.skin.label);
                style.normal.textColor = Color.red;
                style.fontSize = 14;
                style.fontStyle = FontStyle.Bold;

                Handles.Label(pos + new Vector3(0, 2, 0), indicator.Key.ToString(), style);
            }
        }

        public override void DrawSceneHandles(SceneView sv)
        {
            if (Owner.WorkingMapData == null)
            {
                return;
            }

            ProcessPreTasks();
            DrawGrid(sv);
            DrawPathfindingActivationTiles(sv);
            DrawEnemySpawnPoints(sv);
            UpdateSceneObjectEntries(sv);
            RecordBoundaryData(sv);

            SceneView.RepaintAll();
        }

        private void UpdateSceneObjectEntries(SceneView sv)
        {
            foreach (var obj in _sceneMapObjects)
            {
                obj.Value.OnSceneDrawHandles(sv, _drawBoundary, _grid.Width, _grid.Height);
            }
        }

        public override void PopulateMapObjectData(List<EntityObjectData> objectData)
        {
            objectData.Clear();

            foreach (var mapObject in _sceneMapObjects)
            {
                objectData.Add(new EntityObjectData(
                    mapObject.Value.transform.position,
                    (int)mapObject.Value.transform.eulerAngles.y,
                    mapObject.Value.TableID,
                    mapObject.Value.TeamType));
            }

            // 굳이 정렬할 필요 없긋지? 
        }

        public override void BeforeSave()
        {
            base.BeforeSave();
        }

        public override void OnSaved()
        {
            base.OnSaved();

            foreach (var mapObject in _sceneMapObjects)
            {
                mapObject.Value.OnSaved();
            }

            _isEntityModified = false;
            _isEnemySpawnPointDirty = false;
            _pathfindingActivationDataDirty = false;
        }

        public override bool IsDirty()
        {
            foreach (var obj in _sceneMapObjects.Values)
            {
                if (obj.IsDirty)
                {
                    return true;
                }
            }

            if (_isEntityModified)
                return true;

            if (_isEnemySpawnPointDirty)
                return true;

            if (_pathfindingActivationDataDirty)
                return true;

            return false;
        }

        private void ProcessPreTasks()
        {
            var e = Event.current;
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                _pickingIntersected = ToolHelper.RaycastPlane(HandleUtility.GUIPointToWorldRay(e.mousePosition), _floorPlane, out _pickingIntersectionPoint);
                Vector2Int nodeSpacePosition = new Vector2Int(Mathf.FloorToInt(_pickingIntersectionPoint.x), Mathf.FloorToInt(_pickingIntersectionPoint.z));
                _pickingTilePosition = nodeSpacePosition;
            }

            if (_pickingIntersected)
            {
                var prevColor = Handles.color;
                Vector3 center = new Vector3(_pickingTilePosition.x + 0.5f, 0.05f, _pickingTilePosition.y + 0.5f);

                Color mainColor = _grid.IsInside(_pickingTilePosition) ? Color.green : Color.red;

                Handles.color = mainColor;
                Handles.DrawWireCube(center, new Vector3(1, 0.1f, 1));

                Handles.color = prevColor;
            }
        }

        void DrawGrid(SceneView sv)
        {
            _grid.Draw(sv);
        }

        void DrawPathfindingActivationTiles(SceneView sv)
        {
            if (_drawPathfindingInactivatedArea == false)
            {
                return;
            }

            var inactivatedColor = new Color(1f, 0, 0, 0.5f);
            var verts = new Vector3[4];

            for (int i = 0; i < _grid.Count; i++)
            {
                var idx = _grid[i].Position;
                var pos = _grid.IdxToTilePos(idx);
                if (MapUtils.IsInsideArea(pos, Owner.WorkingMapData.activationLeftBottomMapPosition, Owner.WorkingMapData.activationRightTopMapPosition) == false)
                {
                    verts[0] = new Vector3(pos.x, 0, pos.y);
                    verts[1] = new Vector3(pos.x, 0, pos.y + 1);
                    verts[2] = new Vector3(pos.x + 1, 0, pos.y + 1);
                    verts[3] = new Vector3(pos.x + 1, 0, pos.y);
                    Handles.DrawSolidRectangleWithOutline(verts, inactivatedColor, Color.cyan);
                }
            }
        }

        void RecordBoundaryData(SceneView sv)
        {
            if (_isBoundaryRecording == false)
            {
                return;
            }

            var e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                if (e.button == 0)
                {
                    _isLeftMouseDown = true;
                }
                else if (e.button == 1)
                {
                    _isRightMouseDown = true;
                }
            }
            else if (e.type == EventType.MouseUp)
            {
                _isLeftMouseDown = false;
                _isRightMouseDown = false;
            }

            if (_isLeftMouseDown)
            {
                if (_boundaryRecordPositions.Contains(_pickingTilePosition) == false)
                {
                    _boundaryRecordPositions.Add(_pickingTilePosition);
                    e.Use();
                }
            }
            else if (_isRightMouseDown)
            {
                if (_boundaryRecordPositions.Contains(_pickingTilePosition))
                {
                    _boundaryRecordPositions.Remove(_pickingTilePosition);
                    e.Use();
                }
            }

            if (_boundaryRecordPositions != null)
            {
                var oriColor = Handles.color;

                foreach (var pos in _boundaryRecordPositions)
                {
                    // 1. 바닥에서 확실히 띄움 (다른 초록색 위로 올라오도록)
                    float y = 0.1f;
                    Vector3 center = new Vector3(pos.x + 0.5f, y, pos.y + 0.5f);
                    Vector3[] verts = new Vector3[]
                    {
                        new Vector3(pos.x, y, pos.y),
                        new Vector3(pos.x, y, pos.y + 1f),
                        new Vector3(pos.x + 1f, y, pos.y + 1f),
                        new Vector3(pos.x + 1f, y, pos.y)
                    };

                    // 면 색상
                    Handles.color = new Color(0f, 1f, 0.5f, 0.4f);
                    Handles.DrawAAConvexPolygon(verts);

                    // 외곽선 강조
                    Handles.color = new Color(0.2f, 1f, 0.2f, 1f);
                    for (float i = 0f; i <= 0.03f; i += 0.01f)
                    {
                        Handles.DrawWireCube(center, new Vector3(1f + i, 0.05f, 1f + i));
                    }
                }
                Handles.color = oriColor;
            }
        }

        void RefreshTerrain()
        {
            ClearTerrain();
            CreateTerrain();
        }
    }
}
