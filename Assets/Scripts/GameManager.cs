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

    [Header("결과 테스트 모드 (결과 테스트 씬에서만 켠다)")]
    public bool showGoalResultChoice = false; // 목표 달성 시 결과 화면을 띄우고 무한 모드 진입 여부를 묻는다(끄면 기존처럼 자동으로 무한 모드 진행)
    public float testGoalAfterSeconds = 0f; // 0보다 크면 게임 시작 후 이 시간(초)이 지날 때 보스를 처치하지 않아도 목표 달성으로 처리(테스트용)
    public bool awaitingGoalChoice { get; private set; } // 결과 화면에서 무한 모드 진입 여부를 고르는 중

    // 1차 목표 보스(30분 보스)를 처치했을 때 호출: 결과 스냅샷을 한 번 기록하고 무한 모드로 계속 진행
    // 게임 오버 이후에는 처리하지 않아 플레이어 사망과 같은 프레임이면 패배가 우선된다
    public void ReachPrimaryGoal() {
        if (isGameover || primaryGoalReached)
        {
            return;
        }

        primaryGoalReached = true;
        UIManager.instance.RecordPrimaryGoalSnapshot(score);

        // 결과 테스트 모드: 게임을 멈추고 결과 화면에서 무한 모드 진입 여부를 묻는다
        if (showGoalResultChoice)
        {
            awaitingGoalChoice = true;
            isPaused = true;
            Time.timeScale = 0f;
            UIManager.instance.ShowGoalResult(score);
        }
    }

    // 결과 화면의 "무한 모드 진입": 게임을 다시 진행한다
    public void ContinueEndless() {
        if (!awaitingGoalChoice)
        {
            return;
        }

        awaitingGoalChoice = false;
        isPaused = false;
        Time.timeScale = 1f;
        UIManager.instance.HideGoalResult();
    }

    // 결과 화면의 "종료": 무한 모드에 들어가지 않고 이 판을 끝낸다(시간 정지, 다시 시작 또는 선택 화면으로 이동)
    public void FinishAtGoal() {
        if (!awaitingGoalChoice)
        {
            return;
        }

        awaitingGoalChoice = false;
        isGameover = true;
        isPaused = false;
        Time.timeScale = 0f;
        UIManager.instance.ShowGoalFinished();
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

        // HUD 진행 시간 표시(게임 시간 기준이라 일시정지 중에는 멈춘다)
        UIManager.instance.UpdateTimeText(Mathf.FloorToInt(Time.timeSinceLevelLoad));

        // 테스트 모드: 지정한 시간이 지나면 목표 달성으로 처리(결과 화면 확인용)
        if (testGoalAfterSeconds > 0f && !primaryGoalReached && Time.timeSinceLevelLoad >= testGoalAfterSeconds)
        {
            ReachPrimaryGoal();
        }

        // 결과 화면에서 선택하는 동안에는 ESC 일시정지를 받지 않는다
        if (awaitingGoalChoice)
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

    // 진행 불가 오류 상태(사용할 수 있는 스폰 지점이 계속 없는 경우 등): 시뮬레이션을 멈추고 오류 UI와 재시작을 제공
    // 게임 오버 상태로 취급해 입력·점수·생성을 막는다. 이미 종료된 판에서는 무시한다
    public void EnterErrorState(string message) {
        if (isGameover)
        {
            return;
        }

        isGameover = true;
        isPaused = false;
        Time.timeScale = 0f;
        UIManager.instance.SetActivePauseUI(false);
        UIManager.instance.ShowProgressError(message);
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