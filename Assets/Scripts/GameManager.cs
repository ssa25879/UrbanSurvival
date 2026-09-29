using UnityEngine;

// 점수와 게임 오버 여부를 관리하는 게임 매니저
public class GameManager : MonoBehaviour {
    // 싱글톤 접근용 프로퍼티
    public static GameManager instance
    {
        get
        {
            // 만약 싱글톤 변수에 아직 오브젝트가 할당되지 않았다면
            if (m_instance == null)
            {
                // 씬에서 GameManager 오브젝트를 찾아 할당
                m_instance = FindObjectOfType<GameManager>();
            }

            // 싱글톤 오브젝트를 반환
            return m_instance;
        }
    }

    private static GameManager m_instance; // 싱글톤이 할당될 static 변수

    private int score = 0; // 현재 게임 점수
    public bool isGameover { get; private set; } // 게임 오버 상태
    public bool isPaused { get; private set; } // 일시정지 상태
    public KeyCode pauseKey = KeyCode.Escape; // 일시정지 토글 키

    // 생존 경과 시간(분). 신규 생성 적의 시간비례 난이도 배율 계산에 사용(씬 로드 시점 기준, 재시작 시 자동 초기화)
    public float elapsedMinutes => Time.timeSinceLevelLoad / 60f;

    // 누적 점수 1,000점당 기본 피해량 +1% 영구 가산, 상한 없음(초기 제안값)
    public float damageMultiplier => 1f + Mathf.Floor(score / 1000f) * 0.01f;

    public float primaryGoalMinutes = 30f; // 1차 목표 보스가 등장하는 시각(분). 이 시각에 등장하는 보스를 처치하면 목표 달성
    public bool primaryGoalReached { get; private set; } // 1차 목표 달성 여부(한 판에 한 번만 참이 됨)

    // 1차 목표 보스(30분 보스)를 처치했을 때 호출: 결과 스냅샷을 한 번 기록하고 무한 모드로 계속 진행
    // 게임 오버 이후에는 처리하지 않아 플레이어 사망과 같은 프레임이면 패배가 우선된다
    public void ReachPrimaryGoal() {
        if (isGameover || primaryGoalReached)
        {
            return;
        }

        primaryGoalReached = true;
        UIManager.instance.RecordPrimaryGoalSnapshot(score);
    }

    private void Awake() {
        // 씬에 싱글톤 오브젝트가 된 다른 GameManager 오브젝트가 있다면
        if (instance != this)
        {
            // 자신을 파괴
            Destroy(gameObject);
        }
    }

    private void Start() {
        // 플레이어 캐릭터의 사망 이벤트 발생시 게임 오버
        FindObjectOfType<PlayerHealth>().onDeath += EndGame;
    }

    private void Update() {
        // 게임 오버 상태에서는 일시정지를 걸 수 없음(이미 멈춰있고, 재시작만 가능)
        if (isGameover)
        {
            return;
        }

        // 설정 창이 열려 있으면 ESC는 설정 창 닫기에만 사용(같은 프레임에 일시정지까지 풀리지 않도록)
        if (Input.GetKeyDown(pauseKey) && !GameSettingsKit.SettingsPanel.BlocksEscapeThisFrame)
        {
            TogglePause();
        }
    }

    // 일시정지 상태를 토글(Time.timeScale로 이동/물리/발사 쿨타임/좀비 AI 전부 일괄 정지)
    public void TogglePause() {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        UIManager.instance.SetActivePauseUI(isPaused);
    }

    // 점수를 추가하고 UI 갱신
    public void AddScore(int newScore) {
        // 게임 오버가 아닌 상태에서만 점수 증가 가능
        if (!isGameover)
        {
            // 점수 추가
            score += newScore;
            // 점수 UI 텍스트 갱신
            UIManager.instance.UpdateScoreText(score);
        }
    }

    // 게임 오버 처리
    public void EndGame() {
        // 게임 오버 상태를 참으로 변경
        isGameover = true;
        // 혹시 일시정지 중이었다면 시간 배율과 일시정지 UI를 원복(정상적으로는 일시정지 중 사망이 불가능하지만 안전장치로 둠)
        isPaused = false;
        Time.timeScale = 1f;
        UIManager.instance.SetActivePauseUI(false);
        // 게임 오버 UI를 활성화
        UIManager.instance.SetActiveGameoverUI(true);
    }
}