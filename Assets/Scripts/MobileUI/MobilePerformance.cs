using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 모바일 프레임 설정과 개발 빌드 전용 성능 측정 도구
// Android는 기본 30 FPS로 제한되므로 60 FPS를 명시한다(VSync가 켜져 있으면 targetFrameRate가 무시되므로 끈다)
public static class MobilePerformance {
    public const int TargetFrameRate = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplyFrameRate() {
        if (!Application.isMobilePlatform)
        {
            return;
        }

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFrameRate;
    }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateProbe() {
        SceneManager.sceneLoaded += (scene, mode) => EnsureProbe();
        EnsureProbe();
    }

    private static void EnsureProbe() {
        if (!MobilePlatform.IsMobile || Object.FindFirstObjectByType<MobilePerformanceProbe>() != null)
        {
            return;
        }

        if (Object.FindFirstObjectByType<ZombieSpawner>() == null)
        {
            return;
        }

        new GameObject("Mobile Performance Probe").AddComponent<MobilePerformanceProbe>();
    }
#endif
}

#if DEVELOPMENT_BUILD || UNITY_EDITOR
// 좌상단에 FPS(평균·최저 프레임)와 적 수를 표시하고, STRESS 버튼으로 "500마리 + 보스" 상태를 만든다(개발 빌드 전용)
public class MobilePerformanceProbe : MonoBehaviour {
    private Text label;
    private float elapsed;
    private int frames;
    private float worstFrame;
    private string text = "";

    private void Awake() {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        RectTransform safe = MobileUIFactory.NewRect("Safe Area", transform);
        MobileUIFactory.Stretch(safe);
        safe.gameObject.AddComponent<SafeAreaFitter>();

        label = MobileUIFactory.NewText("FPS", safe, "", 30, Color.yellow, TextAnchor.UpperLeft);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0f, 1f);
        label.rectTransform.pivot = new Vector2(0f, 1f);
        label.rectTransform.anchoredPosition = new Vector2(24f, -300f);
        label.rectTransform.sizeDelta = new Vector2(700f, 160f);

        Image background = MobileUIFactory.NewImage("Stress Button", safe, MobileUIFactory.Panel);
        RectTransform rect = background.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -470f);
        rect.sizeDelta = new Vector2(240f, 80f);
        Text buttonText = MobileUIFactory.NewText("Label", rect, "STRESS", 32, MobileUIFactory.Light, TextAnchor.MiddleCenter);
        MobileUIFactory.Stretch(buttonText.rectTransform);
        TouchButton button = background.gameObject.AddComponent<TouchButton>();
        button.background = background;
        button.onDown = StartStress;
    }

    private void Update() {
        float dt = Time.unscaledDeltaTime;
        elapsed += dt;
        frames++;
        worstFrame = Mathf.Max(worstFrame, dt);

        if (elapsed >= 1f)
        {
            text = "FPS " + (frames / elapsed).ToString("F1") + "  worst " + (1f / Mathf.Max(worstFrame, 0.0001f)).ToString("F0")
                + "\nzombies " + Zombie.alive.Count + "  bosses " + Zombie.bosses.Count;
            elapsed = 0f;
            frames = 0;
            worstFrame = 0f;
        }

        label.text = text;
    }

    // 몬스터를 빠르게 늘리고 보스를 곧 등장시키며 플레이어가 죽지 않게 해 부하 상태를 재현한다
    private void StartStress() {
        ZombieSpawner spawner = FindFirstObjectByType<ZombieSpawner>();
        if (spawner != null)
        {
            spawner.waveIntervalMin = 0.3f;
            spawner.waveIntervalMax = 0.6f;
            spawner.baseWaveZombieCount = 60;
            spawner.zombieCountIncreasePerWave = 10;
            spawner.maxConcurrentZombies = 500;
        }

        GameManager manager = GameManager.instance;
        if (manager != null)
        {
            manager.startElapsedMinutes = Mathf.Max(manager.startElapsedMinutes, 9.95f); // 곧 10분 보스가 등장
        }

        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
        if (health != null)
        {
            health.startingHealth = 1000000f;
            health.RestoreHealth(1000000f);
        }
    }
}
#endif
