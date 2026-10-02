using UnityEngine;

// 보스 연습 씬(BossTestScene, FinalBossTestScene)의 진행 담당(2026-10-01 추가)
// - 일반 적 없이 보스 1마리만 소환(ZombieSpawner.spawnNormalWaves / practiceBossMinute로 설정)
// - 보스가 소환된 순간부터 처치까지의 시간을 재서 HUD에 표시하고, 처치하면 기록·최고 기록과 함께 결과 화면을 띄운다
// - 무기 4종 보유는 PlayerShooter.unlockAllWeaponsOnStart, 시작 시간은 GameManager.startElapsedMinutes로 씬에서 설정
public class PracticeBossMode : MonoBehaviour {
    public enum Kind {
        Boss = 0,
        FinalBoss = 1
    }

    public Kind kind = Kind.Boss;
    public ZombieSpawner spawner; // 보스를 소환하는 스포너(onBossSpawned 구독)

    private const string BestKeyPrefix = "PracticeBest_";

    private Zombie boss;
    private float bossStartTime;
    private bool finished;
    private bool timerFrozen; // 게임 오버로 타이머를 멈췄는지(사망 시 Time.timeScale이 1로 유지되어 시간이 계속 흐르기 때문)
    private PlayerShooter shooter;
    private PlayerHealth playerHealth;

    // 인트로의 연습 패널이 최고 기록을 보여 줄 때 사용(기록 없으면 0)
    public static float GetBestSeconds(Kind kind) {
        return PlayerPrefs.GetFloat(BestKeyPrefix + kind, 0f);
    }

    public static string FormatTime(float seconds) {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        float rest = seconds - minutes * 60f;
        return minutes + ":" + rest.ToString("00.0");
    }

    private string Label {
        get { return kind == Kind.Boss ? "BOSS" : "FINAL BOSS"; }
    }

    private void Start() {
        shooter = FindFirstObjectByType<PlayerShooter>();
        playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (spawner != null)
        {
            spawner.onBossSpawned += OnBossSpawned;
        }

        // 탄약이 모자라 처치 시간이 왜곡되지 않도록 예비탄이 바닥나면 채운다
        InvokeRepeating(nameof(RefillAmmo), 0.5f, 0.5f);
    }

    private void OnDestroy() {
        if (spawner != null)
        {
            spawner.onBossSpawned -= OnBossSpawned;
        }
    }

    private void RefillAmmo() {
        if (shooter != null)
        {
            shooter.RefillReserveIfEmpty();
        }
    }

    private void OnBossSpawned(Zombie spawned) {
        if (boss != null)
        {
            return;
        }

        boss = spawned;
        bossStartTime = Time.time;
        boss.onDeath += OnBossDied;
    }

    // 스포너가 쓰는 웨이브 텍스트 대신 연습용 정보를 매 프레임 덮어쓴다(스포너의 UpdateUI 뒤에 실행)
    private void LateUpdate() {
        if (UIManager.instance == null || UIManager.instance.waveText == null || finished)
        {
            return;
        }

        if (boss == null)
        {
            UIManager.instance.waveText.text = "VS " + Label + "\nGET READY";
            return;
        }

        // 플레이어가 사망하거나 진행 불가 상태가 되면 그 순간의 처치 시간·보스 체력을 고정한다(2026-10-02 수정: 사망 뒤에도 시간이 흐르던 문제)
        if (timerFrozen)
        {
            return;
        }

        bool stopped = GameManager.instance != null && GameManager.instance.isGameover;
        float elapsed = Time.time - bossStartTime;
        if (stopped)
        {
            timerFrozen = true;
        }

        UIManager.instance.waveText.text = Label + " TIME " + FormatTime(elapsed)
            + "\nBOSS HP " + Mathf.CeilToInt(Mathf.Max(0f, boss.health)) + " / " + Mathf.CeilToInt(boss.startingHealth);
    }

    private void OnBossDied() {
        // 이미 게임 오버(사망 등)가 된 뒤의 처치는 기록하지 않는다
        if (finished || (GameManager.instance != null && GameManager.instance.isGameover))
        {
            return;
        }

        finished = true;
        float seconds = Time.time - bossStartTime;

        float best = GetBestSeconds(kind);
        bool newRecord = best <= 0f || seconds < best;
        if (newRecord)
        {
            best = seconds;
            PlayerPrefs.SetFloat(BestKeyPrefix + kind, best);
            PlayerPrefs.Save();
        }

        string stats = "KILL TIME : " + FormatTime(seconds)
            + "\nBEST : " + FormatTime(best) + (newRecord ? "  NEW!" : "");
        if (playerHealth != null)
        {
            stats += "\nHP LEFT : " + Mathf.CeilToInt(Mathf.Max(0f, playerHealth.health)) + " / " + Mathf.CeilToInt(playerHealth.startingHealth);
        }

        GameManager.instance.EnterPracticeResult();
        UIManager.instance.ShowPracticeResult(Label + " DOWN", stats);
    }
}
