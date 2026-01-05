using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(GameStateMetaData))]
public class GameStateMetaDataDrawer : PropertyDrawer
{
    private Type[] _targetTypesCache;
    private string[] _typeNamesCache;
    private string[] _typeFullNamesCache;

    // 인스펙터에 프로퍼티를 그리는 메인 함수
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // UI 타입 캐시가 없으면 미리 만들어 둡니다 (성능 최적화)
        if (_targetTypesCache == null)
        {
            FindTargetTypes();
        }

        EditorGUI.BeginProperty(position, label, property);

        // 리스트 요소의 폴드아웃(열고 닫는 화살표)을 그립니다.
        property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), property.isExpanded, label);

        // 폴드아웃이 열려있을 때만 내부 필드를 그립니다.
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            // 'state'와 'scene' 필드는 기본 UI로 간단하게 그립니다.
            var stateProperty = property.FindPropertyRelative("state");
            var sceneProperty = property.FindPropertyRelative("scene");

            // --- 커스텀 UI가 필요한 'uiTypes' 배열 그리기 ---
            var uiTypesProperty = property.FindPropertyRelative("uiTypes");

            // 현재 그릴 Y 위치 계산
            float currentY = position.y + EditorGUIUtility.singleLineHeight;

            // state 필드 그리기
            Rect stateRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(stateRect, stateProperty);
            currentY += EditorGUIUtility.singleLineHeight;

            // scene 필드 그리기
            Rect sceneRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(sceneRect, sceneProperty);
            currentY += EditorGUIUtility.singleLineHeight;

            // uiTypes 배열 헤더 그리기
            Rect uiTypesLabelRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(uiTypesLabelRect, "Ui Types");
            currentY += EditorGUIUtility.singleLineHeight;

            EditorGUI.indentLevel++;

            // uiTypes 배열 크기 조절 필드 그리기
            int newSize = EditorGUI.IntField(new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight), "Size", uiTypesProperty.arraySize);
            if (newSize != uiTypesProperty.arraySize)
            {
                uiTypesProperty.arraySize = newSize;
            }
            currentY += EditorGUIUtility.singleLineHeight;

            // 배열의 각 요소를 드롭다운으로 그리기
            for (int i = 0; i < uiTypesProperty.arraySize; i++)
            {
                SerializedProperty element = uiTypesProperty.GetArrayElementAtIndex(i);

                int selectedIndex = Array.IndexOf(_typeFullNamesCache, element.stringValue);
                if (selectedIndex < 0) selectedIndex = 0;

                Rect popupRect = new Rect(position.x, currentY, position.width, EditorGUIUtility.singleLineHeight);
                int newIndex = EditorGUI.Popup(popupRect, $"Element {i}", selectedIndex, _typeNamesCache);

                if (newIndex < _typeFullNamesCache.Length)
                {
                    element.stringValue = _typeFullNamesCache[newIndex];
                }
                currentY += EditorGUIUtility.singleLineHeight;
            }
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    // 이 프로퍼티를 그리는데 필요한 전체 높이를 계산해서 반환
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float totalHeight = EditorGUIUtility.singleLineHeight; // 기본 폴드아웃 높이

        if (property.isExpanded)
        {
            // 'state', 'scene' 필드 높이
            totalHeight += EditorGUIUtility.singleLineHeight * 2;

            // 'uiTypes' 관련 높이 (헤더 + 사이즈 필드 + 각 요소)
            var uiTypesProperty = property.FindPropertyRelative("uiTypes");
            totalHeight += EditorGUIUtility.singleLineHeight * (2 + uiTypesProperty.arraySize);
        }

        return totalHeight;
    }

    private void FindTargetTypes()
    {
        _targetTypesCache = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsSubclassOf(typeof(UIBase)) && !t.IsAbstract)
            .ToArray();
        _typeNamesCache = _targetTypesCache.Select(t => t.Name).ToArray();
        _typeFullNamesCache = _targetTypesCache.Select(t => t.FullName).ToArray();
    }
}
