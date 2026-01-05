using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using MessagePack;
using MessagePack.Unity;
using Newtonsoft.Json;

namespace Tool
{
    public class MapToolWindow : EditorWindow
    {
        [Serializable]
        class TasksOnReload
        {
            public bool reloadTable;
        }

        Dictionary<MapToolMainMode, MapToolFrameBase<MapToolWindow>> _frames;
        MapToolMainMode _currentMode;
        string[] _allModes;

        TasksOnReload _tasksOnReload = new TasksOnReload();

        public MapToolGameDBContainer TableContainer { get; private set; }
        public bool IsTableReady => TableContainer != null && TableContainer.IsReady;
        DateTime _tableLoadedAt;

        private ToolMapMetadata _toolMapMetadata;
        public ToolMapMetadata MapMetadata => _toolMapMetadata;
        public string[] _mapDataNames;

        public int _currentWorkingMapDataIdx;
        bool _mapListGUIFold;

        public MapData WorkingMapData => _currentWorkingMapDataIdx >= 0 && _currentWorkingMapDataIdx < _toolMapMetadata.MapDataList.Count
            ? _toolMapMetadata.MapDataList[_currentWorkingMapDataIdx].SourceMapData : null;

        Vector2 _mainContentsScroll;

        bool _modified;

        [MenuItem("Tools/Map Tool")]
        public static void Open()
        {
            GetWindow<MapToolWindow>("Map Tool");
        }

        private void OnEnable()
        {
            _allModes = ((MapToolMainMode[])Enum.GetValues(typeof(MapToolMainMode))).Select(t => t.ToString()).ToArray();

            _frames = new Dictionary<MapToolMainMode, MapToolFrameBase<MapToolWindow>>();
            _frames.Add(MapToolMainMode.NotLoaded, new MapNotLoadedFrame(this));
            _frames.Add(MapToolMainMode.Editing, new MapEditFrame(this));

            LoadTable();

            foreach (var frame in _frames)
            {
                frame.Value.OnEnable();
            }

            ChangeMode(_currentMode);

            // TO _dataContainer 복원

            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void Update()
        {
            if (_currentMode == MapToolMainMode.Editing)
            {
                if (_modified == false)
                {
                    _modified = _frames[MapToolMainMode.Editing].IsDirty();
                }
            }
        }

        public bool ChangeMapName(string newName)
        {
            foreach (var map in _toolMapMetadata.MapDataList)
            {
                if (newName == map.SourceMapData.name)
                {
                    EditorUtility.DisplayDialog($"오류", "이름이 중복되는 맵이 있습니다.", "확인");
                    return false;
                }
            }

            var targetMap = _toolMapMetadata.MapDataList[_currentWorkingMapDataIdx];
            var prevPath = ToolHelper.ConvertAbsoluteToAssetPath(targetMap.AbsolutePath);
            targetMap.ChangeName(WorkingMapData.name, newName);
            _mapDataNames = _toolMapMetadata.MapDataList.Select(t => t.SourceMapData.name).ToArray();

            File.WriteAllText(prevPath, JsonUtility.ToJson(targetMap.SourceMapData, prettyPrint: true));
            AssetDatabase.Refresh();

            AssetDatabase.RenameAsset(prevPath, newName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return true;
        }

        public void ChangeMapSize(Vector2Int size)
        {
            if (WorkingMapData == null)
            {
                Debug.LogError("Data is NULL, what changed what size????");
                return;
            }

            _modified = true;

            WorkingMapData.width = (ushort)size.x;
            WorkingMapData.height = (ushort)size.y;

            _frames[_currentMode].OnMapSizeChanged(size);
        }

        public void AddMapObjects(IEnumerable<EntityObjectData> added)
        {
            if (WorkingMapData == null)
            {
                Debug.LogError("Data is NULL, what changed what size????");
                return;
            }

            _modified = true;

            WorkingMapData.objects.AddRange(added);
            _frames[_currentMode].OnMapObjectAdded(added);
        }

        public void ClearMapObjects()
        {
            if (WorkingMapData == null)
            {
                Debug.LogError("Data is NULL, what changed what size????");
                return;
            }
            if (WorkingMapData.objects == null || WorkingMapData.objects.Count == 0)
            {
                return;
            }

            _modified = true;

            WorkingMapData.objects.Clear();
            _frames[_currentMode].OnMapObjectCleared();
        }

        private void OnBeforeAssemblyReload()
        {
            Debug.Log("BEFORE");

            _tasksOnReload.reloadTable = TableContainer.IsReady;

            foreach (var frame in _frames)
            {
                frame.Value.OnBeforeAssemblyReload();
            }
        }

        private void OnAfterAssemblyReload()
        {
            Debug.Log("AFTER");

            GameDBHelper.RegisterMessagePackResolvers();

            if (_tasksOnReload.reloadTable)
            {
                LoadTable();
                _tasksOnReload.reloadTable = false;
            }

            foreach (var frame in _frames)
            {
                frame.Value.OnAfterAssemblyReload();
            }
        }

        private void SetMode(MapToolMainMode mode)
        {
            _currentMode = mode;

        }

        private void OnDisable()
        {
            foreach (var frame in _frames)
            {
                frame.Value.OnDisable();
            }

            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        void OnGUI()
        {
            DrawMenuBar();

            _mainContentsScroll = EditorGUILayout.BeginScrollView(_mainContentsScroll);
            {
                DrawTableManagement();
                DrawMainData();
            }
            EditorGUILayout.EndScrollView();

            _frames[_currentMode].OnGUI();
        }

        private void DrawMainData()
        {
            EditorGUILayout.BeginVertical("box");
            {
                if (_toolMapMetadata != null)
                {
                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("■■■■■ 로드된 메타 파일 정보 ■■■■■");
                    EditorGUILayout.LabelField("버전", _toolMapMetadata.Metadata.SourceMetadata.Version);
                    EditorGUILayout.LabelField("토탈 해쉬", _toolMapMetadata.Metadata.SourceMetadata.TotalHash);
                    EditorGUILayout.LabelField("경로", _toolMapMetadata.Metadata.MetadataAbsolutePath);

                    if (GUILayout.Button("맵 생성하기"))
                    {
                        CreateMapData();
                    }

                    GUI.enabled = _currentMode == MapToolMainMode.Editing;
                    {
                        if (GUILayout.Button("맵 삭제하기"))
                        {
                            if (EditorUtility.DisplayDialog("경고", "되돌릴 수 없습니다. 정말 삭제하시겠습니까?", "삭제", "취소"))
                            {
                                DeleteCurrentMapData();
                            }
                        }
                    }
                    GUI.enabled = true;

                    _mapListGUIFold = EditorGUILayout.Foldout(_mapListGUIFold, $"맵 리스트 ({_toolMapMetadata.Metadata.SourceMetadata.Files.Count}개 존재)");
                    if (_mapListGUIFold)
                    {
                        EditorGUI.indentLevel++;
                        {
                            int newIdx = GUILayout.SelectionGrid(
                                _currentWorkingMapDataIdx,
                                _mapDataNames,
                                4);

                            if (newIdx != _currentWorkingMapDataIdx)
                            {
                                if (EditorUtility.DisplayDialog("주의", "작업 맵을 바꾸시겠습니까?", "변경", "취소"))
                                {
                                    _currentWorkingMapDataIdx = newIdx;

                                    if (_currentMode == MapToolMainMode.Editing)
                                    {
                                        _frames[_currentMode].OnMapLoadedNew(_toolMapMetadata.MapDataList[newIdx].SourceMapData.objects);
                                    }
                                    else
                                    {
                                        ChangeMode(MapToolMainMode.Editing);
                                    }

                                    SceneView.RepaintAll();
                                }
                            }
                        }
                        EditorGUI.indentLevel--;
                    }

                    if (WorkingMapData != null)
                    {
                        EditorGUILayout.LabelField("■■ 작업중인 맵 정보 ■■");

                        EditorGUILayout.LabelField($"맵 사이즈 가로: {WorkingMapData.width} 세로 : {WorkingMapData.height}");
                        if (GUILayout.Button("수정하기"))
                        {
                            Vector2 mousePos = Event.current.mousePosition;
                            Rect popupRect = new Rect(
                                mousePos,
                                Vector2.zero
                            );
                            PopupWindow.Show(popupRect, new ModifyMapContentPopup(
                                WorkingMapData,
                                (result) =>
                                {
                                    if (WorkingMapData.name != result.name)
                                    {
                                        ChangeMapName(result.name);
                                        _modified = true;
                                    }

                                    if (WorkingMapData.width != result.width ||
                                    WorkingMapData.height != result.height)
                                    {
                                        ChangeMapSize(new Vector2Int(result.width, result.height));
                                        _modified = true;
                                    }

                                    if (WorkingMapData.terrainMaterialKey !=
                                    result.terrainMaterialKey)
                                    {
                                        ChangeTerrainMaterial(result.terrainMaterialKey);
                                        _modified = true;
                                    }
                                }));
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("현재 작업중인 맵이 없습니다.");
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("로드된 메타 파일 없음");
                }
            }
            EditorGUILayout.EndVertical();
        }

        void ChangeTerrainMaterial(string key)
        {
            WorkingMapData.terrainMaterialKey = key;
            _frames[_currentMode].OnTerrainMaterialChanged(key);
        }

        private void OnSceneGUI(SceneView sv)
        {
            _frames[_currentMode].DrawSceneHandles(sv);

            Handles.BeginGUI();
            {
                DrawSaveButton(sv);

                _frames[_currentMode].DrawSceneGUI(sv);
            }
            Handles.EndGUI();
        }

        private void DrawMenuBar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 필요하다면 드롭다운 메뉴로 확장
            if (GUILayout.Button("Options ▾", EditorStyles.toolbarDropDown))
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("맵 메타데이터 생성하기"), false, CreateMetadata);
                menu.AddItem(new GUIContent("맵 메타데이터 로드하기"), false, LoadMetadata);
                menu.DropDown(new Rect(10, 0, 100, EditorGUIUtility.singleLineHeight));
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawTableManagement()
        {
            EditorGUILayout.BeginVertical();
            {
                string metadataPath = EditorPrefs.GetString(MapToolConstants.TableMetadataPathPrefKey);
                string binDir = EditorPrefs.GetString(MapToolConstants.TableBinDirectoryPrefKey);
                bool tryLoad = false;

                EditorGUILayout.LabelField($"테이블 로드 상태 여부 : {IsTableReady}");
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.LabelField($"현재 메타데이터 파일경로 : {metadataPath}");
                        if (GUILayout.Button("경로 선택"))
                        {
                            string redirectDir = File.Exists(metadataPath) ? Path.GetDirectoryName(metadataPath) : Application.dataPath;
                            string selectedPath = EditorUtility.OpenFilePanel("메타데이터 파일 경로", redirectDir, "JSON files,json");
                            if (string.IsNullOrEmpty(selectedPath) == false)
                            {
                                metadataPath = selectedPath;
                                EditorPrefs.SetString(MapToolConstants.TableMetadataPathPrefKey, selectedPath);
                                tryLoad = !string.IsNullOrEmpty(binDir);
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.LabelField($"현재 테이블 파일 폴더경로 : {binDir}");
                        if (GUILayout.Button("경로 선택"))
                        {
                            string redirectDir = Directory.Exists(binDir) ? binDir : Application.dataPath;
                            string selectedDir = EditorUtility.OpenFolderPanel("테이블 폴더 경로", redirectDir, "");
                            if (string.IsNullOrEmpty(selectedDir) == false)
                            {
                                binDir = selectedDir;
                                EditorPrefs.SetString(MapToolConstants.TableBinDirectoryPrefKey, selectedDir);
                                tryLoad = !string.IsNullOrEmpty(metadataPath);
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();

                if (tryLoad)
                {
                    if (LoadTable() == false)
                    {
                        EditorUtility.DisplayDialog("로드 실패", "테이블 로드에 실패했습니다. 경로를 확인해주세요.", "확인");
                    }
                }

                EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(metadataPath) || string.IsNullOrEmpty(binDir));
                {
                    if (GUILayout.Button("재로드 시도"))
                    {
                        if (LoadTable() == false)
                        {
                            EditorUtility.DisplayDialog("로드 실패", "테이블 로드에 실패했습니다. 경로를 확인해주세요.", "확인");
                        }
                    }
                }
                EditorGUI.EndDisabledGroup();

                if (IsTableReady)
                    EditorGUILayout.LabelField($"테이블이 정상적으로 로드되었습니다.({_tableLoadedAt})");
                else
                    EditorGUILayout.LabelField("테이블이 로드되지 않았습니다.");
            }
            EditorGUILayout.EndVertical();
        }

        void DrawSaveButton(SceneView sv)
        {
            var style = EditorStyles.toolbarButton;
            var btnContent = new GUIContent("Save");
            var btnSize = style.CalcSize(btnContent);
            var rect = new Rect(
                sv.position.width - btnSize.x - 10,
                sv.position.height - btnSize.y * 2 - 10,
                btnSize.x,
                btnSize.y);
            bool prevEnabled = GUI.enabled;
            GUI.enabled = _modified;
            if (GUI.Button(rect, btnContent))
            {
                Save();
            }
            GUI.enabled = prevEnabled;
        }

        bool LoadTable()
        {
            var metadataPath = EditorPrefs.GetString(MapToolConstants.TableMetadataPathPrefKey);
            var binDirectory = EditorPrefs.GetString(MapToolConstants.TableBinDirectoryPrefKey);

            TableContainer = new MapToolGameDBContainer();
            bool isTableReady = TableContainer.InitializeTable(metadataPath, binDirectory);

            if (isTableReady)
            {
                TEMP_Logger.Deb($"Table Loaded");
                _tableLoadedAt = DateTime.Now;
                return true;
            }

            TEMP_Logger.Err($"Table Load Failed | MetaDataPath : {metadataPath} , BinDir : {binDirectory}");
            return false;
        }

        void Save()
        {
            if (_toolMapMetadata == null || _toolMapMetadata.Metadata == null || _toolMapMetadata.Metadata.SourceMetadata == null)
            {
                Debug.LogError($"No Data to Save !");
                return;
            }

            // Save 를 진행하기 전에 먼저, EditFrame 에서 수정된거로 데이터 최신화
            _frames[MapToolMainMode.Editing].PopulateMapObjectData(WorkingMapData.objects);

            _frames[MapToolMainMode.Editing].BeforeSave();

            // 주어진 맵 데이터들을 기반으로 메타데이터 갱신 
            _toolMapMetadata.RefreshInfo();

            // 맵 데이터들 파일 저장 
            foreach (var map in _toolMapMetadata.MapDataList)
            {
                var json = JsonUtility.ToJson(map.SourceMapData, prettyPrint: true);

                File.WriteAllText(map.AbsolutePath, json);
            }

            // 메타데이터 파일 저장 
            File.WriteAllText(_toolMapMetadata.Metadata.MetadataAbsolutePath, JsonUtility.ToJson(_toolMapMetadata.Metadata.SourceMetadata, prettyPrint: true));

            AssetDatabase.Refresh();

            _frames[_currentMode].OnSaved();
            _modified = false;
        }

        void CreateMetadata()
        {
            string absolutePath = EditorUtility.SaveFilePanel("Save Metadata", Application.dataPath, Path.GetFileNameWithoutExtension(Constants.Paths.MapDataMetadataFileName), "json");
            if (string.IsNullOrEmpty(absolutePath)) return;

            foreach (var otherFile in Directory.GetFiles(Path.GetDirectoryName(absolutePath)))
            {
                if (otherFile.EndsWith(".json"))
                {
                    EditorUtility.DisplayDialog("오류", @$"해당 메타데이터 디렉터리 상에 다른 .json 파일이 발견되었습니다. 잠재적인 메타데이터 충돌 방지를 위해
한 폴더안의 맵 데이터 메타데이터는 하나만 존재할 수 있습니다. (하위 폴더(../{Constants.Paths.MapDataSubDirectoryNameFromMetadata})
를 자동으로 맵 데이터 폴더로 인식하기 위한 시스템)", "확인");
                    return;
                }
            }

            var newMetadata = new MapDataMetadata();
            string json = JsonUtility.ToJson(newMetadata, prettyPrint: true);
            File.WriteAllText(absolutePath, json);
            AssetDatabase.Refresh();

            ReloadMetaData(absolutePath);
        }

        void LoadMetadata()
        {
            string absolutePath = EditorUtility.OpenFilePanel("맵 메타데이터 선택"
                , _toolMapMetadata != null ? ToolHelper.ConvertAbsoluteToAssetPath(_toolMapMetadata.Metadata.MapDataAbsoluteDir)
                : "Assets", "json");
            if (string.IsNullOrEmpty(absolutePath))
            {
                return;
            }

            if (absolutePath.StartsWith(Application.dataPath) == false)
            {
                EditorUtility.DisplayDialog("오류", "현재 프로젝트의 맵 메타 데이터만 가능합니다.", "확인");
                return;
            }

            if (_currentMode == MapToolMainMode.Editing)
            {
                if (_modified)
                {
                    bool res = EditorUtility.DisplayDialog("경고", "현재 작업중인 맵 데이터가 저장되지 않았습니다. 새로 로드하시겠습니까?", "로드", "취소");
                    if (res == false)
                        return;
                }
            }

            ReloadMetaData(absolutePath);
        }

        void ReloadMetaData(string absolutePath)
        {
            _toolMapMetadata = new ToolMapMetadata();

            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(ToolHelper.ConvertAbsoluteToAssetPath(absolutePath));

            _toolMapMetadata.Metadata.SourceMetadata = JsonUtility.FromJson<MapDataMetadata>(asset.text);
            _toolMapMetadata.Metadata.TextAsset = asset;
            _toolMapMetadata.Metadata.MetadataAbsolutePath = absolutePath;

            string hashForDifferenceCheck = _toolMapMetadata.Metadata.SourceMetadata.TotalHash;

            var rootDir = Path.GetDirectoryName(absolutePath);

            // 맵데이터도 다 같이 로드
            var mapDataAbsDir = Path.Combine(rootDir, Constants.Paths.MapDataSubDirectoryNameFromMetadata);
            _toolMapMetadata.Metadata.MapDataAbsoluteDir = mapDataAbsDir;
            if (Directory.Exists(mapDataAbsDir))
            {
                string mapDataProjectDir = ToolHelper.ConvertAbsoluteToAssetPath(mapDataAbsDir);
                string[] mapDataGuids = AssetDatabase.FindAssets("t:TextAsset", new string[] { mapDataProjectDir });

                foreach (var mapDataGuid in mapDataGuids)
                {
                    var mapTxtAsset = AssetDatabase.LoadAssetByGUID<TextAsset>(new GUID(mapDataGuid));
                    if (mapTxtAsset == null)
                    {
                        EditorUtility.DisplayDialog("오류", $"올바른 맵 파일 ({nameof(MapData)}) 을 선택해주세요.", "확인");
                        return;
                    }

                    var loadedMapData = JsonUtility.FromJson<MapData>(mapTxtAsset.text);

                    AddMapDataToList(new ToolMapMetadata.Map()
                    {
                        SourceMapData = loadedMapData,
                        TextAsset = mapTxtAsset,
                        AbsolutePath = Path.Combine(mapDataAbsDir, mapTxtAsset.name + ".json")
                    });
                }
            }
            else
            {
                Directory.CreateDirectory(mapDataAbsDir);
            }

            _toolMapMetadata.RefreshInfo();

            // 만약 , 읽어들였던 메타데이터의 TotalHash 가 실제 데이터를 읽고 난 후의 TotalHash 와 다르다면 ,
            // 이는 메타데이터가 기존에 저장됐을떄의 맵 데이터에 변동이 생겼던 것이므로 새로 Write 해준다 
            if (hashForDifferenceCheck != _toolMapMetadata.Metadata.SourceMetadata.TotalHash)
            {
                File.WriteAllText(_toolMapMetadata.Metadata.MetadataAbsolutePath, JsonUtility.ToJson(_toolMapMetadata.Metadata.SourceMetadata, prettyPrint: true));
                AssetDatabase.Refresh();
                Debug.LogWarning($"기존 메타데이터의 내용과(TotalHash) 실제 TotalHash 가 다릅니다. 실제 내용으로 메타데이터를 기록합니다.");
            }
            _currentWorkingMapDataIdx = -1;
            ChangeMode(MapToolMainMode.NotLoaded);

            _mapDataNames = _toolMapMetadata.MapDataList.Select(t => t.SourceMapData.name).ToArray();
            SceneView.lastActiveSceneView.showGrid = false;
            Selection.activeObject = asset;
            _modified = false;
        }

        void CreateMapData()
        {
            if (_toolMapMetadata == null)
            {
                TEMP_Logger.Err($"Metadata not exist error");
                return;
            }

            //string absolutePath = EditorUtility.SaveFilePanel("Save Map Data", Application.dataPath, "NewMapData", "json");
            //if (string.IsNullOrEmpty(absolutePath)) return;

            int numbering = 1;
            string defaultName = "MapData";
            string newMapName = defaultName + numbering.ToString("D2");

            while (_toolMapMetadata.MapDataList.Exists(t => t.SourceMapData.name == newMapName))
            {
                newMapName = defaultName + (numbering++).ToString("D2");
            }

            var newMapData = new MapData()
            {
                id = 1,
                name = newMapName,
                width = 100,
                height = 100,
                terrainMaterialKey = "SomeMaterial",
                objects = new List<EntityObjectData>()
            };

            string json = JsonUtility.ToJson(newMapData, prettyPrint: true);
            string mapDataAbsolutePath = Path.Combine(_toolMapMetadata.Metadata.MapDataAbsoluteDir, newMapData.name + ".json");
            File.WriteAllText(mapDataAbsolutePath, json);
            AssetDatabase.Refresh();

            var toProjectPath = ToolHelper.ConvertAbsoluteToAssetPath(mapDataAbsolutePath);

            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(toProjectPath);

            AddMapDataToList(new ToolMapMetadata.Map()
            {
                SourceMapData = newMapData,
                TextAsset = textAsset,
                AbsolutePath = mapDataAbsolutePath
            });

            _toolMapMetadata.RefreshInfo();

            Selection.activeObject = textAsset;
        }

        void DeleteCurrentMapData()
        {
            if (_toolMapMetadata == null || _currentWorkingMapDataIdx < 0)
            {
                TEMP_Logger.Err("Nothing to delete error");
                return;
            }

            File.Delete(_toolMapMetadata.MapDataList[_currentWorkingMapDataIdx].AbsolutePath);
            AssetDatabase.Refresh();
            _toolMapMetadata.MapDataList.RemoveAt(_currentWorkingMapDataIdx);
            ReloadMetaData(_toolMapMetadata.Metadata.MetadataAbsolutePath);
        }

        void AddMapDataToList(ToolMapMetadata.Map map)
        {
            _toolMapMetadata.MapDataList.Add(map);
            _mapDataNames = _toolMapMetadata.MapDataList.Select(t => t.SourceMapData.name).ToArray();
        }

        bool RemoveMapData(ToolMapMetadata.Map map)
        {
            if (WorkingMapData == map.SourceMapData)
            {
                EditorUtility.DisplayDialog("오류", "작업중인 맵은 삭제 불가", "확인");
                return false;
            }

            bool res = _toolMapMetadata.MapDataList.Remove(map);
            _mapDataNames = _toolMapMetadata.MapDataList.Select(t => t.SourceMapData.name).ToArray();
            return res;
        }

        void ChangeMode(MapToolMainMode mode)
        {
            _frames[_currentMode].OnExit();
            _frames[mode].OnEnter();
            _currentMode = mode;
        }
    }
}
