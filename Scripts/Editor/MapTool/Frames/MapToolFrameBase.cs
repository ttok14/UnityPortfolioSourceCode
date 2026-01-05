using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tool
{
    [Serializable]
    public abstract class MapToolFrameBase<OwnerType> where OwnerType : EditorWindow
    {
        protected OwnerType Owner { get; private set; }

        public MapToolFrameBase(OwnerType owner)
        {
            Owner = owner;
        }

        public virtual void OnEnter()
        {
        }

        public virtual void OnExit()
        {

        }

        public virtual void Initialize()
        {

        }

        public virtual void OnEnable()
        {
        }

        public virtual void OnDisable()
        {
        }

        public virtual void PopulateMapObjectData(List<EntityObjectData> objectData)
        {

        }

        public virtual void BeforeSave()
        {

        }

        public virtual void OnSaved()
        {

        }

        public virtual bool IsDirty()
        {
            return false;
        }

        public virtual void OnBeforeAssemblyReload()
        {

        }

        public virtual void OnAfterAssemblyReload()
        {

        }

        public virtual void OnMapLoadedNew(IEnumerable<EntityObjectData> list)
        {

        }

        public virtual void OnMapSizeChanged(Vector2Int newSize)
        {

        }

        public virtual void OnMapObjectAdded(IEnumerable<EntityObjectData> added)
        {

        }

        public virtual void OnMapObjectRemoved(IEnumerable<EntityObjectData> removed)
        {

        }

        public virtual void OnMapObjectCleared()
        {

        }

        public virtual void OnGUI()
        {

        }

        public virtual void DrawSceneGUI(SceneView sv)
        {

        }

        public virtual void DrawSceneHandles(SceneView sv)
        {

        }

        public virtual void OnTerrainMaterialChanged(string key)
        {

        }
    }
}
