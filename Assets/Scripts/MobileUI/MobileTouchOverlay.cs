using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 모바일 터치 오버레이: 이동 스틱, 발사 버튼(오토 에임) 또는 조준 스틱(쌍둥이 스틱), 재장전, 무기 슬롯 4개, 일시정지 버튼
// 게임 시스템과 분리되어 있고 MobileInputState에 값만 쓴다. 씬에 PlayerInput이 있을 때만 모바일에서 자동 생성된다
public class MobileTouchOverlay : MonoBehaviour
{
    private const string ObjectName = "Mobile Touch Overlay";
    private const float MoveDeadZone = 0.2f;
    private const float AimDeadZone = 0.25f;
    private const float SlotLineOwned = 3f;
    private const float SlotLineSelected = 7f;

    private static readonly Color SlotSelectedColor = new Color(0.19f, 0.155f, 0.08f, 0.9f);
    private static readonly Color SlotLockedColor = new Color(0.07f, 0.08f, 0.09f, 0.45f);
    private static readonly Color SlotOwnedLine = new Color(0.93f, 0.74f, 0.36f, 0.45f);
    private static readonly Color SlotLockedLine = new Color(0.45f, 0.45f, 0.45f, 0.3f);

    private RectTransform controlsRoot; // 정지·게임오버 때 숨길 컨트롤 묶음(일시정지 버튼 제외)
    private GameObject fireButtonObject;
    private GameObject aimZoneObject;
    private TouchJoystick moveStick;
    private TouchJoystick aimStick;
    private TouchButton fireButton;
    private TouchButton reloadButton;
    private TouchButton pauseButton;
    private readonly TouchButton[] slotButtons = new TouchButton[4];
    private readonly Text[] slotNames = new Text[4];
    private readonly Image[] slotLines = new Image[4];
    private PlayerShooter playerShooter;
    private PlayerInput playerInput;
    private GameObject pauseButtonObject;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded += (scene, mode) => TryCreate();
        TryCreate();
    }

    private static void TryCreate()
    {
        if (!MobilePlatform.IsMobile || FindFirstObjectByType<MobileTouchOverlay>() != null)
        {
            return;
        }

        if (FindFirstObjectByType<PlayerInput>() == null)
        {
            return; // 인트로 등 플레이어가 없는 씬에는 만들지 않는다
        }

        new GameObject(ObjectName).AddComponent<MobileTouchOverlay>();
    }

    private void Awake()
    {
        playerShooter = FindFirstObjectByType<PlayerShooter>();
        playerInput = FindFirstObjectByType<PlayerInput>();
        BuildCanvas();
        gameObject.AddComponent<MobileHudLayout>(); // 총기·탄약 패널을 상단 중앙으로 옮긴다
        ApplyAimMode(MobileAimSettings.Mode, false);
        MobileAimSettings.Changed += OnAimModeChanged;
    }

    private void OnDestroy()
    {
        MobileAimSettings.Changed -= OnAimModeChanged;
        MobileInputState.ResetAll();
    }

    private void OnAimModeChanged(MobileAimMode mode)
    {
        ApplyAimMode(mode, true);
    }

    // 조준 모드에 맞게 발사 버튼/조준 스틱을 전환하고, 남은 발사·조준 입력은 초기화한다
    private void ApplyAimMode(MobileAimMode mode, bool resetInput)
    {
        bool twin = mode == MobileAimMode.TwinStick;
        fireButtonObject.SetActive(!twin);
        aimZoneObject.SetActive(twin);

        if (resetInput)
        {
            fireButton.Release();
            aimStick.ResetStick();
            if (playerInput != null)
            {
                playerInput.RequireFireRelease();
            }
            else
            {
                MobileInputState.ResetAll();
            }
        }
    }

    private void BuildCanvas()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        RectTransform safe = MobileUIFactory.NewRect("Safe Area", transform);
        MobileUIFactory.Stretch(safe);
        safe.gameObject.AddComponent<SafeAreaFitter>();

        controlsRoot = MobileUIFactory.NewRect("Controls", safe);
        MobileUIFactory.Stretch(controlsRoot);

        BuildMoveZone();
        BuildAimZone();
        BuildFireButton();
        BuildReloadButton();
        BuildWeaponSlots();
        BuildPauseButton(safe);
    }

    // 왼쪽 아래 이동 스틱 영역(화면 왼쪽 45%, 아래 80%). 위쪽은 HUD를 가리지 않도록 비운다
    private void BuildMoveZone()
    {
        RectTransform zone = NewZone("Move Zone", new Vector2(0f, 0f), new Vector2(0.45f, 0.8f));
        moveStick = CreateStick(zone, MoveDeadZone);
        moveStick.onValue = value => MobileInputState.Move = value;
    }

    // 오른쪽 아래 조준 스틱 영역(쌍둥이 스틱 모드). 화면 오른쪽 45%, 아래 80%
    private void BuildAimZone()
    {
        RectTransform zone = NewZone("Aim Zone", new Vector2(0.55f, 0f), new Vector2(1f, 0.8f));
        aimZoneObject = zone.gameObject;
        aimStick = CreateStick(zone, AimDeadZone);
        aimStick.onValue = value => MobileInputState.AimStick = value;
    }

    private RectTransform NewZone(string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform zone = MobileUIFactory.NewRect(name, controlsRoot);
        zone.anchorMin = anchorMin;
        zone.anchorMax = anchorMax;
        zone.offsetMin = Vector2.zero;
        zone.offsetMax = Vector2.zero;
        zone.pivot = new Vector2(0.5f, 0.5f);
        Image hit = zone.gameObject.AddComponent<Image>();
        hit.color = new Color(0f, 0f, 0f, 0f); // 보이지 않지만 터치는 받는다
        return zone;
    }

    private TouchJoystick CreateStick(RectTransform zone, float deadZone)
    {
        Image baseImage = MobileUIFactory.NewImage("Base", zone, new Color(1f, 1f, 1f, 0.16f), MobileUIFactory.Circle);
        baseImage.raycastTarget = false;
        baseImage.rectTransform.sizeDelta = new Vector2(240f, 240f);
        Image knob = MobileUIFactory.NewImage("Knob", baseImage.transform,
            new Color(MobileUIFactory.Amber.r, MobileUIFactory.Amber.g, MobileUIFactory.Amber.b, 0.85f),
            MobileUIFactory.Circle);
        knob.raycastTarget = false;
        knob.rectTransform.sizeDelta = new Vector2(110f, 110f);

        TouchJoystick stick = zone.gameObject.AddComponent<TouchJoystick>();
        stick.radius = 110f;
        stick.deadZone = deadZone;
        stick.baseImage = baseImage.rectTransform;
        stick.knob = knob.rectTransform;
        stick.ResetStick(); // AddComponent 시점의 Awake에서는 바탕이 아직 연결 전이라, 연결 뒤 직접 숨긴다
        return stick;
    }

    // 오른쪽 아래 발사 버튼(오토 에임 모드)
    private void BuildFireButton()
    {
        // 오른쪽 아래 모서리(기존 총기·탄약 패널이 있던 자리. 탄약 표시는 상단 중앙으로 옮겼다)
        fireButton = CreatePanelButton("Fire Button", controlsRoot, new Vector2(-134f, 134f), 220f, "FIRE", 48, 8f);
        fireButtonObject = fireButton.gameObject;
        fireButton.onDown = () =>
        {
            MobileInputState.FireHeld = true;
            MobileInputState.RequestFireDown();
        };
        fireButton.onUp = () => MobileInputState.FireHeld = false;
    }

    // 발사 버튼 왼쪽 재장전 버튼(발사 버튼과 아래쪽 정렬)
    private void BuildReloadButton()
    {
        reloadButton = CreatePanelButton("Reload Button", controlsRoot, new Vector2(-329f, 89f), 130f, "R", 52, 6f);
        reloadButton.onDown = MobileInputState.RequestReload;
    }

    // HUD 패널 스타일(둥근 사각 패널 + 상단 앰버 라인) 정사각 버튼. 누르는 동안 앰버 채움 + 어두운 글자
    private TouchButton CreatePanelButton(string name, Transform parent, Vector2 anchoredFromBottomRight, float size,
        string label, int fontSize, float lineThickness)
    {
        Image background = MobileUIFactory.NewPanel(name, parent, MobileUIFactory.Panel);
        RectTransform rect = background.rectTransform;
        PlaceBottomRight(rect, anchoredFromBottomRight, size);
        MobileUIFactory.NewTopLine(rect, lineThickness, MobileUIFactory.Amber);

        Text text = MobileUIFactory.NewText("Label", rect, label, fontSize, MobileUIFactory.Light,
            TextAnchor.MiddleCenter);
        MobileUIFactory.Stretch(text.rectTransform);

        TouchButton button = background.gameObject.AddComponent<TouchButton>();
        button.background = background;
        button.label = text;
        return button;
    }

    private static void PlaceBottomRight(RectTransform rect, Vector2 anchoredPosition, float size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(size, size);
    }

    // 발사·재장전 버튼 위쪽 가로 한 줄 무기 슬롯 4개(PISTOL / AR / SMG / SG, 왼쪽부터 슬롯 1~4)
    private void BuildWeaponSlots()
    {
        for (int i = 0; i < 4; i++)
        {
            int slotIndex = i;

            Image background = MobileUIFactory.NewPanel("Slot " + (i + 1), controlsRoot, MobileUIFactory.Panel);
            RectTransform rect = background.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-24f - (3 - i) * 170f, 302f);
            rect.sizeDelta = new Vector2(160f, 84f);
            slotLines[i] = MobileUIFactory.NewTopLine(rect, SlotLineOwned, MobileUIFactory.Amber);

            // 축약 이름만 표시(번호 배지 없음)
            Text nameText = MobileUIFactory.NewText("Name", rect, WeaponDisplayName.SlotShortLabel(i), 32,
                MobileUIFactory.Light, TextAnchor.MiddleCenter);
            MobileUIFactory.Stretch(nameText.rectTransform);
            nameText.rectTransform.offsetMin = new Vector2(6f, 0f);
            nameText.rectTransform.offsetMax = new Vector2(-6f, 0f);
            slotNames[i] = nameText;

            TouchButton button = background.gameObject.AddComponent<TouchButton>();
            button.background = background;
            button.disabledColor = SlotLockedColor;
            button.onDown = () =>
            {
                if (playerShooter != null && playerShooter.IsSlotUnlocked(slotIndex))
                {
                    MobileInputState.RequestSwap(slotIndex);
                }
            };
            slotButtons[i] = button;
        }
    }

    // 오른쪽 위 일시정지 버튼(안전 영역 안). 정지 중에는 기존 메뉴가 계속하기를 담당하므로 숨긴다
    private void BuildPauseButton(RectTransform safe)
    {
        Image background = MobileUIFactory.NewPanel("Pause Button", safe, MobileUIFactory.Panel);
        RectTransform rect = background.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-380f, -24f);
        rect.sizeDelta = new Vector2(110f, 110f);
        MobileUIFactory.NewTopLine(rect, 5f, MobileUIFactory.Amber);

        // "II" 글리프는 글꼴에 따라 모양이 다를 수 있어 두 개의 세로 막대로 직접 그린다
        Image[] bars = new Image[2];
        for (int i = 0; i < 2; i++)
        {
            bars[i] = MobileUIFactory.NewImage("Bar " + (i + 1), rect, MobileUIFactory.Light);
            bars[i].raycastTarget = false;
            RectTransform barRect = bars[i].rectTransform;
            barRect.anchorMin = barRect.anchorMax = new Vector2(0.5f, 0.5f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.anchoredPosition = new Vector2(i == 0 ? -14f : 14f, -4f);
            barRect.sizeDelta = new Vector2(14f, 46f);
        }

        pauseButton = background.gameObject.AddComponent<TouchButton>();
        pauseButton.background = background;
        pauseButtonObject = background.gameObject;
        pauseButton.onDown = () =>
        {
            GameManager manager = GameManager.instance;
            if (manager != null && !manager.isGameover && !manager.isPaused && !manager.awaitingGoalChoice)
            {
                manager.TogglePause();
            }
        };
    }

    private void Update()
    {
        GameManager manager = GameManager.instance;
        bool blocked = manager != null && (manager.isPaused || manager.isGameover || manager.awaitingGoalChoice);
        if (controlsRoot.gameObject.activeSelf == blocked)
        {
            controlsRoot.gameObject.SetActive(!blocked);
        }

        if (pauseButtonObject.activeSelf == blocked)
        {
            pauseButtonObject.SetActive(!blocked);
        }

        if (blocked || playerShooter == null)
        {
            return;
        }

        reloadButton.SetInteractable(true);
        for (int i = 0; i < 4; i++)
        {
            bool unlocked = playerShooter.IsSlotUnlocked(i);
            bool selected = playerShooter.CurrentSlotIndex == i;
            // 3가지 상태: 선택(굵은 앰버 라인·앰버 글자·따뜻한 배경) / 보유(밝은 글자) / 미보유(어둡고 흐리게)
            slotButtons[i].normalColor = selected ? SlotSelectedColor : MobileUIFactory.Panel;
            slotButtons[i].SetInteractable(unlocked);
            MobileUIFactory.SetTopLineThickness(slotLines[i], selected ? SlotLineSelected : SlotLineOwned);
            slotLines[i].color = !unlocked ? SlotLockedLine : (selected ? MobileUIFactory.Amber : SlotOwnedLine);
            slotNames[i].color = !unlocked
                ? MobileUIFactory.Dim
                : (selected ? MobileUIFactory.Amber : MobileUIFactory.Light);
        }
    }
}