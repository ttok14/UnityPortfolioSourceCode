using System;
using UnityEngine;
using GameDB;
using UnityEditor;
using System.Collections.Generic;

public class AddEntityContentPopup : PopupWindowContent
{
    // id , pos , euler
    Action<uint, Vector3, int, EntityTeamType> _cb;

    int _entityId;

    Vector3 _entityPosition;
    int _eulerY;
    EntityTeamType _teamType;

    bool _modified;

    Dictionary<uint, EntityTable> _tableDic;

    public AddEntityContentPopup(Dictionary<uint, EntityTable> tableDic, Action<uint, Vector3, int, EntityTeamType> cb)
    {
        _cb = cb;
        _tableDic = tableDic;
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
            EditorGUI.BeginChangeCheck();
            {
                _entityId = EditorGUILayout.IntField($"생성할 ID", _entityId);
                _entityPosition = EditorGUILayout.Vector3Field($"월드 위치", _entityPosition);
                _eulerY = EditorGUILayout.IntField($"오일러 Y", _eulerY);
                _teamType = (EntityTeamType)EditorGUILayout.EnumPopup("팀", _teamType);
            }
            _modified = EditorGUI.EndChangeCheck() ? true : _modified;

            GUI.enabled = _modified;
            if (GUILayout.Button("추가하기"))
            {
                if (_tableDic.ContainsKey((uint)_entityId) == false)
                {
                    EditorUtility.DisplayDialog("오류", "존재하지 않는 ID임", "확인");
                    return;
                }

                if (EditorUtility.DisplayDialog("확인", "정말 추가하시겠습니까?", "추가", "취소"))
                {
                    _cb.Invoke((uint)_entityId, _entityPosition, _eulerY, _teamType);
                    editorWindow.Close();
                }
            }
            GUI.enabled = true;
        }
        GUILayout.EndVertical();
    }
}
