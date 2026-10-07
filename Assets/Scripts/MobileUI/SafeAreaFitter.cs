using UnityEngine;

// 노치·펀치홀·둥근 모서리를 피하도록 UI 컨테이너를 Screen.safeArea 범위로 맞춘다(월드 렌더링 영역은 줄이지 않는다)
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour {
    private RectTransform rect;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void Awake() {
        rect = (RectTransform)transform;
        Apply();
    }

    private void Update() {
        if (Screen.safeArea != lastSafeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
        {
            Apply();
        }
    }

    private void Apply() {
        Rect safe = Screen.safeArea;
        lastSafeArea = safe;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
