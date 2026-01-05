using UnityEngine;

// 게임 내에서 FPS를 표시하는 간단한 컴포넌트
public class FrameDisplay : MonoBehaviour
{
    [Header("설정")]
    public Color textColor = Color.white;  // 글자 색상
    public int fontSize = 20;              // 글자 크기
    public Vector2 position = new Vector2(10, 10); // 좌측 상단 위치

    private float deltaTime = 0.0f;

    void Update()
    {
        // FPS 측정을 위한 델타타임 보정 (지수평균)
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        // GUI 스타일 설정
        GUIStyle style = new GUIStyle();
        Rect rect = new Rect(position.x, position.y, Screen.width, fontSize + 10);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = fontSize;
        style.normal.textColor = textColor;

        // FPS 계산
        float fps = 1.0f / deltaTime;
        string text = $"{fps:0.} FPS";

        // 화면에 출력
        GUI.Label(rect, text, style);
    }
}
