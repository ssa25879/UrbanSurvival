using UnityEngine;
using UnityEngine.UI;

// 모바일 터치 UI를 런타임에 코드로 만들기 위한 보조 함수(바이너리 씬을 수정하지 않기 위함)
// 색은 기존 HUD 톤(어두운 패널 + 앰버 강조 + 패널 상단 앰버 라인)을 따른다.
// 패널 스프라이트와 글꼴은 씬의 WeaponHUD(무기 패널)에서 읽어 와 HUD와 같은 모양을 쓴다(없으면 기본 모양으로 대체)
public static class MobileUIFactory
{
    public static readonly Color Panel = new Color(0.07f, 0.08f, 0.09f, 0.82f); // HUD 패널 색
    public static readonly Color Amber = new Color(0.93f, 0.74f, 0.36f, 1f);
    public static readonly Color Light = new Color(0.95f, 0.95f, 0.93f, 1f);
    public static readonly Color Dim = new Color(0.45f, 0.45f, 0.45f, 0.55f);

    private static Sprite circle;

    // HUD에서 읽어 온 패널 스타일(찾으면 캐시)
    private static bool hudResolved;
    private static Sprite hudPanelSprite;
    private static Image.Type hudPanelType = Image.Type.Simple;
    private static float hudPanelPixelsPerUnit = 1f;
    private static Font hudFont;

    // 128x128 흰색 원(가장자리 부드럽게). 색은 Image.color로 입힌다
    public static Sprite Circle
    {
        get
        {
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

    // 씬의 WeaponHUD(무기 패널)에서 패널 스프라이트·Image.Type·글꼴을 가져온다. 아직 못 찾았으면 다음 호출에서 다시 시도한다
    private static void ResolveHudStyle()
    {
        if (hudResolved)
        {
            return;
        }

        WeaponHUD hud = Object.FindFirstObjectByType<WeaponHUD>();
        if (hud == null)
        {
            return;
        }

        Image panel = hud.GetComponent<Image>();
        if (panel == null)
        {
            panel = hud.GetComponentInParent<Image>();
        }

        if (panel != null && panel.sprite != null)
        {
            hudPanelSprite = panel.sprite;
            hudPanelType = panel.type;
            hudPanelPixelsPerUnit = panel.pixelsPerUnitMultiplier;
        }

        if (hud.weaponNameText != null)
        {
            hudFont = hud.weaponNameText.font;
        }

        hudResolved = true;
    }

    public static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    public static Image NewImage(string name, Transform parent, Color color, Sprite sprite = null)
    {
        RectTransform rect = NewRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    // HUD 패널과 같은 둥근 사각 패널(슬라이스 스프라이트). HUD를 못 찾으면 단색 사각형
    public static Image NewPanel(string name, Transform parent, Color color)
    {
        ResolveHudStyle();
        Image image = NewImage(name, parent, color, hudPanelSprite);
        if (hudPanelSprite != null)
        {
            image.type = hudPanelType;
            image.pixelsPerUnitMultiplier = hudPanelPixelsPerUnit;
        }

        return image;
    }

    // 패널 상단의 얇은 앰버 라인(HUD 패널과 같은 모양). 모서리 둥근 부분을 피해 좌우를 안으로 들인다
    public static Image NewTopLine(RectTransform panel, float thickness, Color color)
    {
        Image line = NewImage("Top Line", panel, color);
        line.raycastTarget = false;
        RectTransform rect = line.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        SetTopLineThickness(line, thickness);
        return line;
    }

    public static void SetTopLineThickness(Image line, float thickness)
    {
        RectTransform rect = line.rectTransform;
        rect.offsetMin = new Vector2(14f, -4f - thickness);
        rect.offsetMax = new Vector2(-14f, -4f);
    }

    public static Text NewText(string name, Transform parent, string text, int fontSize, Color color,
        TextAnchor anchor)
    {
        ResolveHudStyle();
        RectTransform rect = NewRect(name, parent);
        Text label = rect.gameObject.AddComponent<Text>();
        label.font = hudFont != null ? hudFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = anchor;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        return label;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
