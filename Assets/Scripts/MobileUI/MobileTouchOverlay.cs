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

    private RectTransform controlsRoot; // 정지·게임오버 때 숨길 컨트롤 묶음(일시정지 버튼 제외)
    private GameObject fireButtonObject;
    private GameObject fireRingObject;
    private GameObject aimZoneObject;
    private TouchJoystick moveStick;
    private TouchJoystick aimStick;
    private TouchButton fireButton;
    private TouchButton reloadButton;
    private TouchButton pauseButton;
    private readonly TouchButton[] slotButtons = new TouchButton[4];
    private readonly Text[] slotLabels = new Text[4];
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
        fireRingObject.SetActive(!twin);
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
        fireButton = CreateRoundButton("Fire Button", controlsRoot, new Vector2(-640f, 170f), 220f, "FIRE", 40);
        fireButtonObject = fireButton.gameObject;
        fireRingObject = controlsRoot.Find("Fire Button Ring").gameObject; // 링은 버튼의 형제라 함께 전환해야 한다
        fireButton.onDown = () =>
        {
            MobileInputState.FireHeld = true;
            MobileInputState.RequestFireDown();
        };
        fireButton.onUp = () => MobileInputState.FireHeld = false;
    }

    // 발사 버튼 위쪽 재장전 버튼
    private void BuildReloadButton()
    {
        reloadButton = CreateRoundButton("Reload Button", controlsRoot, new Vector2(-430f, 330f), 130f, "R", 44);
        reloadButton.onDown = MobileInputState.RequestReload;
    }

    private TouchButton CreateRoundButton(string name, Transform parent, Vector2 anchoredFromBottomRight, float size,
        string label, int fontSize)
    {
        // 테두리 링은 버튼 배경의 형제로 먼저 만든다(자식으로 두면 배경을 덮어 버튼이 단색이 된다)
        Image ring = MobileUIFactory.NewImage(name + " Ring", parent, MobileUIFactory.Amber, MobileUIFactory.Circle);
        ring.raycastTarget = false;
        PlaceBottomRight(ring.rectTransform, anchoredFromBottomRight, size + 8f);

        Image background = MobileUIFactory.NewImage(name, parent, MobileUIFactory.Panel, MobileUIFactory.Circle);
        RectTransform rect = background.rectTransform;
        PlaceBottomRight(rect, anchoredFromBottomRight, size);

        Text text = MobileUIFactory.NewText("Label", rect, label, fontSize, MobileUIFactory.Light,
            TextAnchor.MiddleCenter);
        MobileUIFactory.Stretch(text.rectTransform);

        TouchButton button = background.gameObject.AddComponent<TouchButton>();
        button.background = background;
        return button;
    }

    private static void PlaceBottomRight(RectTransform rect, Vector2 anchoredPosition, float size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(size, size);
    }

    // 오른쪽 가장자리 세로 배치 무기 슬롯 4개(1=권총, 2=소총, 3=SMG, 4=산탄총)
    private void BuildWeaponSlots()
    {
        string[] names = { "1 PISTOL", "2 RIFLE", "3 SMG", "4 SHOTGUN" };
        for (int i = 0; i < 4; i++)
        {
            int slotIndex = i;
            Image background = MobileUIFactory.NewImage("Slot " + (i + 1), controlsRoot, MobileUIFactory.Panel);
            RectTransform rect = background.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-24f, 620f - i * 100f);
            rect.sizeDelta = new Vector2(230f, 88f);

            Text text = MobileUIFactory.NewText("Label", rect, names[i], 30, MobileUIFactory.Light,
                TextAnchor.MiddleCenter);
            MobileUIFactory.Stretch(text.rectTransform);
            slotLabels[i] = text;

            TouchButton button = background.gameObject.AddComponent<TouchButton>();
            button.background = background;
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
        Image background =
            MobileUIFactory.NewImage("Pause Button", safe, MobileUIFactory.Panel, MobileUIFactory.Circle);
        RectTransform rect = background.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-380f, -24f);
        rect.sizeDelta = new Vector2(110f, 110f);
        Text text = MobileUIFactory.NewText("Label", rect, "II", 44, MobileUIFactory.Light, TextAnchor.MiddleCenter);
        MobileUIFactory.Stretch(text.rectTransform);

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
            slotButtons[i].SetInteractable(unlocked);
            // 선택·보유 상태는 배경색을 직접 갱신한다(누르고 있는 동안의 색은 TouchButton이 담당)
            if (!slotButtons[i].Pressed)
            {
                slotButtons[i].background.color = !unlocked
                    ? MobileUIFactory.Dim
                    : (selected ? MobileUIFactory.Amber : MobileUIFactory.Panel);
            }

            slotLabels[i].color = !unlocked ? MobileUIFactory.Dim : (selected ? Color.black : MobileUIFactory.Light);
        }
    }
}