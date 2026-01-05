using System;
using UnityEngine;
using UnityEditor;

public class ModifyMapContentPopup : PopupWindowContent
{
    Action<MapData> _cb;
    MapData _copied;

    bool _modified = false;

    public ModifyMapContentPopup(MapData sourceMapData, Action<MapData> cb)
    {
        _copied = sourceMapData.Copy(false);
        _cb = cb;
    }

    public override Vector2 GetWindowSize()
    {
        return new Vector2(400, 300);
    }

    public override void OnGUI(Rect rect)
    {
        base.OnGUI(rect);

        GUILayout.BeginVertical("box");
        {
            EditorGUILayout.LabelField($"ID : {_copied.id}");

            EditorGUI.BeginChangeCheck();
            {
                _copied.name = EditorGUILayout.TextField(_copied.name);
                _copied.width = EditorGUILayout.IntField("가로", _copied.width);
                _copied.height = EditorGUILayout.IntField("세로", _copied.height);
                _copied.terrainMaterialKey = EditorGUILayout.TextField("지형 매터리얼 어드레서블 키", _copied.terrainMaterialKey);
            }
            _modified = EditorGUI.EndChangeCheck() ? true : _modified;

            GUI.enabled = _modified;
            if (GUILayout.Button("수정하기"))
            {
                if (EditorUtility.DisplayDialog("확인", "정말 수정하시겠습니까?", "수정", "취소"))
                {
                    _cb.Invoke(_copied);
                    editorWindow.Close();
                }
            }
            GUI.enabled = true;
        }
        GUILayout.EndVertical();
    }
}
