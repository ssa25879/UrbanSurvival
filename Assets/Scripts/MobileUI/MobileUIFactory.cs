using UnityEngine;
using UnityEngine.UI;

// 모바일 터치 UI를 런타임에 코드로 만들기 위한 보조 함수(바이너리 씬을 수정하지 않기 위함)
// 색은 기존 HUD 톤(어두운 패널 + 앰버 강조)을 따른다
public static class MobileUIFactory {
    public static readonly Color Panel = new Color(0.07f, 0.075f, 0.085f, 0.72f);
    public static readonly Color Amber = new Color(0.93f, 0.74f, 0.36f, 1f);
    public static readonly Color Light = new Color(0.95f, 0.95f, 0.93f, 1f);
    public static readonly Color Dim = new Color(0.45f, 0.45f, 0.45f, 0.55f);

    private static Sprite circle;

    // 128x128 흰색 원(가장자리 부드럽게). 색은 Image.color로 입힌다
    public static Sprite Circle {
        get {
            if (circle == null)
            {
                const int size = 128;
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.wrapMode = TextureWrapMode.Clamp;
                float center = (size - 1) * 0.5f;
                float radius = size * 0.5f - 1f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                        float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
                texture.Apply();
                circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                circle.name = "MobileCircle";
            }
            return circle;
        }
    }

    public static RectTransform NewRect(string name, Transform parent) {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    public static Image NewImage(string name, Transform parent, Color color, Sprite sprite = null) {
        RectTransform rect = NewRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    public static Text NewText(string name, Transform parent, string text, int fontSize, Color color, TextAnchor anchor) {
        RectTransform rect = NewRect(name, parent);
        Text label = rect.gameObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = anchor;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        return label;
    }

    public static void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
