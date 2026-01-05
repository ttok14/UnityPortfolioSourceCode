using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Tool
{
    public class MapNotLoadedFrame : MapToolFrameBase<MapToolWindow>
    {
        MapEditTaskMode _currentMode;
        MapEditTaskMode[] _modes;

        Vector2Int _createSize;

        public MapNotLoadedFrame(MapToolWindow parent) : base(parent)
        {

        }

        public override void OnEnable()
        {
            base.OnEnable();
            _modes = (MapEditTaskMode[])System.Enum.GetValues(typeof(MapEditTaskMode));
        }
        public override void OnGUI()
        {
            base.OnGUI();

            if (_currentMode == MapEditTaskMode.Create)
                DrawCreateGUI();
            else if (_currentMode == MapEditTaskMode.Editing)
                DrawEditGUI();
            else
                Debug.LogError($"not implemented type : {_currentMode}");
        }

        private void DrawCreateGUI()
        {
            EditorGUILayout.BeginVertical();
            {
                _createSize = EditorGUILayout.Vector2IntField("맵 사이즈", _createSize);

            }
            EditorGUILayout.EndVertical();
        }

        private void DrawEditGUI()
        {
        }

        public override void DrawSceneGUI(SceneView sv)
        {
            base.DrawSceneGUI(sv);


        }
    }
}
