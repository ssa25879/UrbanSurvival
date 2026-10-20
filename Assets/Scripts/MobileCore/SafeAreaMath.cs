using UnityEngine;

// 안전 영역(Screen.safeArea, 픽셀)을 캔버스 앵커(0~1)로 바꾸는 계산(순수 로직)
public static class SafeAreaMath {
    // 화면 크기가 0 이하이면 전체 화면(0,0)-(1,1)을 돌려준다. 안전 영역이 화면 밖으로 나가도 0~1로 맞춘다
    public static void ToAnchors(Rect safe, int screenWidth, int screenHeight, out Vector2 anchorMin, out Vector2 anchorMax) {
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            anchorMin = Vector2.zero;
            anchorMax = Vector2.one;
            return;
        }

        anchorMin = new Vector2(Mathf.Clamp01(safe.xMin / screenWidth), Mathf.Clamp01(safe.yMin / screenHeight));
        anchorMax = new Vector2(Mathf.Clamp01(safe.xMax / screenWidth), Mathf.Clamp01(safe.yMax / screenHeight));
    }
}
