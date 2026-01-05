
#if UNITY_EDITOR

using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using GameDB;

namespace Tool
{
    public class CustomAddressableAssetEntry
    {
        public string group;
        public string assetPath;
        public string address;
        public string guid;
        public UnityEngine.Object mainAsset;
        //  public MapObjectType objectType;

        public CustomAddressableAssetEntry(string group, string assetPath, string address, string guid, UnityEngine.Object mainAsset /*, MapObjectType objectType*/)
        {
            this.group = group;
            this.assetPath = assetPath;
            this.address = address;
            this.guid = guid;
            this.mainAsset = mainAsset;
            // this.objectType = objectType;
        }
    }

    public class EntityObjectEditor : MonoBehaviour
    {
        [System.Flags]
        public enum DirtyFlag
        {
            None = 0,

            Added = 0x1,
            TransformChanged = 0x1 << 1,
        }

        public CustomAddressableAssetEntry Entry;
        // public EntityObjectData Data;
        public Vector3[] OccupiedWorldPositions;
        public EntityTable TableData;

        public EntityTeamType TeamType;

        public DirtyFlag CurrentDirtyFlag { get; private set; } = DirtyFlag.None;
        public bool IsDirty => CurrentDirtyFlag != DirtyFlag.None;

        private static ulong _nextInstanceIDCount = 0;
        public static ulong NextInstanceID
        {
            get
            {
                ++_nextInstanceIDCount;
                return _nextInstanceIDCount;
            }
        }
        public ulong InstanceID { get; private set; }

        // Collider _collider;

        uint _tableId;
        // public MapObjectType MapObjectType => ToMapObject();

        Vector3 _lastSavedPosition;
        float _lastSavedEulerY;

        Vector3 _lastOccupyUpdatedPosition;
        float _lastOccupyUpdatedEulerY;

        public uint TableID
        {
            get => _tableId;
            set
            {
                _tableId = value;
            }
        }

        public void Setup(bool addedDirty, uint tableId, EntityTable tableData, ulong instanceId, EntityTeamType teamType, CustomAddressableAssetEntry entry)
        {
            if (addedDirty)
            {
                CurrentDirtyFlag |= DirtyFlag.Added;
            }

            _tableId = tableId;
            Entry = entry;
            InstanceID = instanceId;
            TableData = tableData;
            TeamType = teamType;
            _lastSavedPosition = transform.position;
            _lastSavedEulerY = transform.eulerAngles.y;
            // _collider = GetComponent<Collider>();
        }

        //MapObjectType ToMapObject()
        //{
        //    foreach (var kv in ObjectTypeAndInitialNameMap)
        //    {
        //        if (kv.Value.Exists(t => gameObject.name.Contains(t)))
        //        {
        //            return kv.Key;
        //        }
        //    }

        //    Debug.LogError($"Not matching MapObject , addr key : {_entry.address}");
        //    return MapObjectType.None;
        //}

        //public static Dictionary<MapObjectType, List<string>> ObjectTypeAndInitialNameMap = new Dictionary<MapObjectType, List<string>>()
        //{
        //    [MapObjectType.Environment] = new List<string>()
        //    {
        //        "Bush",
        //        "Stone",
        //        "Rock",
        //        "Mountain",
        //        "Flower",
        //        "Grass",
        //        "Tree",
        //        "Foxtail",
        //    },
        //    [MapObjectType.Structure] = new List<string>()
        //    {
        //        "House",
        //        "Fence",
        //        "Siege",
        //        "Tower",
        //        "Store",
        //        "Battering",
        //        "Tent",
        //        "Catapult",
        //    },
        //    [MapObjectType.Prop] = new List<string>()
        //    {
        //        "Signpost",
        //        "Manger",
        //        "Carrier",
        //        "Wagon",
        //        "Pot",
        //        "Fire",
        //        "LightStand",
        //        "Chimney",
        //        "Book",
        //        "Bottle",
        //        "Bowl",
        //        "Beerglass",
        //        "Bucket",
        //        "GunnyBag",
        //        "Bench",
        //        "Bed",
        //        "Candle",
        //        "Candel",
        //        "Brazier",
        //        "Bellows",
        //        "Box",
        //        "Bundle",
        //        "Feather",
        //        "Blacksmith",
        //        "Cook",
        //        "Barrel",
        //        "Cup",
        //        "Candelabrum",
        //        "Chaff",
        //        "Wagon",
        //        "Manger",
        //        "Hourglass",
        //        "Frypan",
        //        "Dish",
        //    },
        //    [MapObjectType.Food] = new List<string>()
        //    {
        //        "Cheese",
        //        "Bread",
        //        "Cucumber",
        //        "Carrot",
        //        "Egg",
        //        "Greenonion",
        //    },
        //    [MapObjectType.Weapon] = new List<string>()
        //    {
        //        "Hammer",
        //        "Ax",
        //        "Chisel",

        //    }
        //};

        public void OnSaved()
        {
            CurrentDirtyFlag = DirtyFlag.None;
            _lastSavedPosition = transform.position;
            _lastSavedEulerY = transform.eulerAngles.y;
        }

        GUIStyle _fontStyle;
        public void OnSceneDrawHandles(SceneView sv, bool drawBoundaries, int width, int height)
        {
            if (_fontStyle == null)
            {
                _fontStyle = new GUIStyle()
                {
                    fontSize = 30,
                    fontStyle = FontStyle.Normal,
                    normal = new GUIStyleState() { textColor = Color.yellow }
                };
            }

            // 이거 해야하나? 확실치않으니 일단 보류
            // Event.current.type == EventType.Repaint

            // 내부적으로 실시간 상태 비교해서 자동 dirty 상태 설정
            if (_lastSavedPosition != transform.position)
            {
                CurrentDirtyFlag |= DirtyFlag.TransformChanged;
            }
            if (_lastSavedEulerY != transform.eulerAngles.y)
            {
                CurrentDirtyFlag |= DirtyFlag.TransformChanged;
            }

            bool updateOccupyOffsets = _lastOccupyUpdatedPosition != transform.position || _lastOccupyUpdatedEulerY != transform.eulerAngles.y;
            if (updateOccupyOffsets)
            {
                _lastOccupyUpdatedPosition = transform.position;
                _lastOccupyUpdatedEulerY = transform.eulerAngles.y;

                UpdateOccupyOffsets(width, height);
            }

            if (drawBoundaries && OccupiedWorldPositions != null)
            {
                Handles.color = new Color(0.7f, 0, 0.5f, 1f);

                foreach (var pos in OccupiedWorldPositions)
                {
                    if (pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height)
                    {
                        var tilePos = MapUtils.WorldPosToTilePos(pos);
                        Vector3 center = new Vector3(tilePos.x + 0.5f, 0, tilePos.y + 0.5f);

                        Vector3[] verts = new Vector3[]
                        {
                            new Vector3(center.x - 0.5f, 0, center.z - 0.5f),
                            new Vector3(center.x - 0.5f, 0, center.z + 0.5f),
                            new Vector3(center.x + 0.5f, 0, center.z + 0.5f),
                            new Vector3(center.x + 0.5f, 0, center.z - 0.5f)
                        };

                        //Handles.DrawAAConvexPolygon(verts);
                        Handles.DrawWireCube(center, new Vector3(1f, 1f, 1f));

                        //Handles.color = Color.green;
                        //Handles.DrawPolyLine(verts[0], verts[1], verts[2], verts[3], verts[0]);

                    }
                }

                Handles.color = new Color(0, 1, 0, 0.2f);
            }

            if (TableData == null)
            {
                Debug.LogError($"Entity Table data does not exist  : " + TableID);
            }
            else
            {
                if (TableData.EntityType == E_EntityType.Structure)
                {
                    //float heightOffset = _collider ? _collider.bounds.extents.y : 1;
                    //if (!_collider)
                    //{
                    //    Debug.LogError($"This Sturcture needs Collider for runtime");
                    //}
                    Handles.Label(transform.position + new Vector3(0, 5, 0), $"ID : {this.TableID}", _fontStyle);
                }
                else
                {
                    if (Selection.activeGameObject == gameObject)
                    {
                        Handles.Label(transform.position + new Vector3(0, 2, 0), $"ID : {this.TableID}", _fontStyle);
                    }
                }
            }
        }

        void UpdateOccupyOffsets(int width, int height)
        {
            var tilePos = MapUtils.WorldPosToTilePos(transform.position);
            if (tilePos.x < 0 || tilePos.x >= width || tilePos.y < 0 || tilePos.y >= height)
            {
                OccupiedWorldPositions = null;
                return;
            }

            if (TableData.OccupyOffsets != null)
            {
                if (OccupiedWorldPositions == null)
                {
                    OccupiedWorldPositions = new Vector3[TableData.OccupyOffsets.Length];
                }

                for (int i = 0; i < TableData.OccupyOffsets.Length; i++)
                {
                    OccupiedWorldPositions[i] = transform.TransformPoint(new Vector3(TableData.OccupyOffsets[i].x, 0, TableData.OccupyOffsets[i].y));
                }
            }
        }
    }
}
#endif
