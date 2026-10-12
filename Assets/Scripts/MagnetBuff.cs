using UnityEngine;
using UnityEngine.UI;

// 자석 버프(2026-10-08, 1.0.3): 획득 후 duration초 동안 플레이어 중심 수평 radius m 안의 아이템을 모두 끌어온다.
// 끌어오는 연출은 기존 ItemMagnet(0.3초 비행)을 그대로 쓰고, 각 아이템의 습득 범위(3 m)는 건드리지 않는다.
// 남은 시간은 Time.deltaTime 기준이라 일시정지(timeScale 0) 중에는 멈추고, 사망하면 끝난다.
// 플레이어 프리팹·씬을 수정하지 않도록 MagnetPickup이 처음 쓰일 때 플레이어에 런타임으로 붙는다.
// 화면 표시: HUD(아이콘 + 남은 시간 + 줄어드는 바)와 플레이어 발밑 범위 원
[RequireComponent(typeof(PlayerHealth))]
public class MagnetBuff : MonoBehaviour {
    public float duration = 10f; // 버프 지속 시간(초)
    public float radius = 10f; // 아이템을 끌어오는 수평 반경(m)

    private const float ScanInterval = 0.1f;
    private const float RingHeight = 0.06f; // 도로면(0.012)·연석 위로 살짝 띄운다

    private static readonly Color Amber = new Color(0.93f, 0.74f, 0.36f, 1f);
    private static readonly Color PanelColor = new Color(0.07f, 0.08f, 0.09f, 0.88f);

    private readonly MagnetBuffTimer timer = new MagnetBuffTimer();
    private PlayerHealth player;
    private PlayerShooter shooter;
    private float nextScanTime;
    private int lastShownTenths = -1; // HUD 텍스트를 값이 바뀔 때만 갱신하기 위한 마지막 표시값(0.1초 단위)

    private Sprite ringSprite;
    private Sprite iconSprite;
    private Transform ring;
    private SpriteRenderer ringRenderer;

    private GameObject hudCanvas;
    private GameObject hudRoot;
    private Text hudText;
    private Image hudBar;

    // 대상(플레이어)에 MagnetBuff가 없으면 붙여서 돌려준다
    public static MagnetBuff GetOrAdd(GameObject target) {
        if (target == null || target.GetComponent<PlayerHealth>() == null)
        {
            return null;
        }

        MagnetBuff buff = target.GetComponent<MagnetBuff>();
        if (buff == null)
        {
            buff = target.AddComponent<MagnetBuff>();
        }

        return buff;
    }

    private void Awake() {
        player = GetComponent<PlayerHealth>();
        shooter = GetComponent<PlayerShooter>();
    }

    // 획득: 남은 시간을 duration으로 되돌린다(중첩해도 누적하지 않음)
    public void Activate(Sprite ringSpriteToUse, Sprite iconSpriteToUse) {
        if (ringSpriteToUse != null) ringSprite = ringSpriteToUse;
        if (iconSpriteToUse != null) iconSprite = iconSpriteToUse;

        timer.Refresh(duration);
        nextScanTime = 0f;
        lastShownTenths = -1;
        EnsureRing();
        EnsureHud();
        SetVisuals(true);
        UpdateVisuals();
    }

    private void Update() {
        if (!timer.IsActive)
        {
            return;
        }

        if (player == null || player.dead)
        {
            EndBuff();
            return;
        }

        timer.Tick(Time.deltaTime);
        if (!timer.IsActive)
        {
            EndBuff();
            return;
        }

        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + ScanInterval;
            PullItems();
        }

        UpdateVisuals();
    }

    private void OnDisable() {
        // 플레이어가 비활성화될 때 화면 표시를 정리한다
        timer.Clear();
        SetVisuals(false);
    }

    private void OnDestroy() {
        if (ring != null) Destroy(ring.gameObject);
        if (hudCanvas != null) Destroy(hudCanvas);
    }

    private void EndBuff() {
        timer.Clear();
        SetVisuals(false);
    }

    // 범위 안의 아이템(비행 중이 아닌 것)에 자석 비행을 시작시킨다
    private void PullItems() {
        Vector3 origin = transform.position;
        float radiusSqr = radius * radius;
        var items = ItemMagnet.All;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            ItemMagnet item = items[i];
            if (item == null || item.IsFlying)
            {
                continue;
            }

            Vector3 delta = item.transform.position - origin;
            delta.y = 0f;
            if (delta.sqrMagnitude <= radiusSqr && CanBenefit(item))
            {
                item.Begin(player);
            }
        }
    }

    // 체력·탄약이 가득 찼을 때는 회복·탄약 상자를 끌어오지 않는다(남겨 두었다가 필요해지면 버프 중에 끌어온다)
    private bool CanBenefit(ItemMagnet item) {
        if (item.GetComponent<HealthPack>() != null)
        {
            return MagnetPullRules.ShouldPullHealthPack(player.health, player.startingHealth);
        }

        if (item.GetComponent<AmmoPack>() != null)
        {
            return shooter != null && shooter.CanUseAmmoPack();
        }

        return true;
    }

    private void SetVisuals(bool visible) {
        if (ring != null) ring.gameObject.SetActive(visible);
        if (hudRoot != null) hudRoot.SetActive(visible);
    }

    private void UpdateVisuals() {
        if (ring != null)
        {
            Vector3 position = transform.position;
            position.y = RingHeight;
            ring.position = position;

            // 끝나기 직전(3초)에는 깜빡여 곧 끝남을 알린다
            Color color = Amber;
            float alpha = 0.28f;
            if (timer.Remaining < 3f)
            {
                alpha = Mathf.Lerp(0.10f, 0.28f, (Mathf.Sin(Time.unscaledTime * 10f) + 1f) * 0.5f);
            }
            color.a = alpha;
            ringRenderer.color = color;
        }

        if (hudText != null)
        {
            // 0.1초 단위가 바뀔 때만 문자열을 만들고 텍스트를 갱신한다(매 프레임 할당·재구성 방지)
            int tenths = Mathf.CeilToInt(timer.Remaining * 10f);
            if (tenths != lastShownTenths)
            {
                lastShownTenths = tenths;
                hudText.text = "MAGNET  " + (tenths / 10f).ToString("0.0") + "s";
            }
        }
        if (hudBar != null)
        {
            hudBar.fillAmount = Mathf.Clamp01(timer.Remaining / duration);
        }
    }

    // 플레이어 발밑 범위 원(지름 radius*2). 플레이어 레이어에 두어 미니맵 지형 카메라에는 찍히지 않는다
    private void EnsureRing() {
        if (ring != null || ringSprite == null)
        {
            return;
        }

        GameObject go = new GameObject("MagnetRange");
        int layer = LayerMask.NameToLayer("Player");
        if (layer >= 0) go.layer = layer;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        ringRenderer = go.AddComponent<SpriteRenderer>();
        ringRenderer.sprite = ringSprite;
        ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ringRenderer.receiveShadows = false;

        float spriteWorldSize = ringSprite.bounds.size.x;
        float scale = radius * 2f / Mathf.Max(spriteWorldSize, 0.01f);
        go.transform.localScale = new Vector3(scale, scale, 1f);

        ring = go.transform;
    }

    // 좌상단(점수·웨이브 패널 아래)에 작은 패널: 아이콘, 남은 시간, 줄어드는 바
    private void EnsureHud() {
        if (hudRoot != null)
        {
            return;
        }

        hudCanvas = new GameObject("MagnetBuffHud");
        Canvas canvas = hudCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        CanvasScaler scaler = hudCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f); // 기존 HUD Canvas와 같은 기준(1280x720, 높이에 맞춤)
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        hudRoot = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        RectTransform rect = (RectTransform)hudRoot.transform;
        rect.SetParent(hudCanvas.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -192f); // 웨이브 패널(WavePanel, y -120~-184) 바로 아래
        rect.sizeDelta = new Vector2(300f, 56f);
        Image background = hudRoot.GetComponent<Image>();
        background.color = PanelColor;
        background.raycastTarget = false;

        // 패널 상단 앰버 라인(기존 HUD 톤)
        Image topLine = NewImage("Top Line", rect, Amber);
        SetRect(topLine.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 3f));

        if (iconSprite != null)
        {
            Image icon = NewImage("Icon", rect, Amber);
            SetRect(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(30f, 2f), new Vector2(34f, 34f));
            icon.sprite = iconSprite;
            icon.preserveAspect = true;
        }

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(56f, 10f);
        textRect.offsetMax = new Vector2(-10f, -6f);
        hudText = textObject.GetComponent<Text>();
        hudText.font = ResolveFont();
        hudText.fontSize = 20;
        hudText.alignment = TextAnchor.MiddleLeft;
        hudText.color = new Color(0.95f, 0.95f, 0.93f, 1f);
        hudText.raycastTarget = false;
        hudText.horizontalOverflow = HorizontalWrapMode.Overflow;

        // 남은 시간 바(아래쪽, 오른쪽에서 왼쪽으로 줄어듦)
        Image barBack = NewImage("Bar Back", rect, new Color(1f, 1f, 1f, 0.12f));
        SetRect(barBack.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(-20f, 6f));
        hudBar = NewImage("Bar", barBack.rectTransform, Amber);
        SetRect(hudBar.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        hudBar.sprite = ResolveBarSprite();
        hudBar.type = Image.Type.Filled;
        hudBar.fillMethod = Image.FillMethod.Horizontal;
        hudBar.fillOrigin = 0;
    }

    private static Image NewImage(string name, RectTransform parent, Color color) {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta) {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    // 기존 HUD 글꼴(웨이브 텍스트)을 쓴다. 없으면 기본 글꼴
    private static Font ResolveFont() {
        if (UIManager.instance != null && UIManager.instance.waveText != null && UIManager.instance.waveText.font != null)
        {
            return UIManager.instance.waveText.font;
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static Sprite whiteSprite;

    // Filled 이미지는 스프라이트가 있어야 채워 보이므로 1x1 흰색 스프라이트를 만든다
    private static Sprite ResolveBarSprite() {
        if (whiteSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            whiteSprite.name = "MagnetBarWhite";
        }

        return whiteSprite;
    }
}
