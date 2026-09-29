using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리자 관련 코드
using UnityEngine.UI; // UI 관련 코드

// 필요한 UI에 즉시 접근하고 변경할 수 있도록 허용하는 UI 매니저
public class UIManager : MonoBehaviour {
    // 싱글톤 접근용 프로퍼티
    public static UIManager instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = FindObjectOfType<UIManager>();
            }

            return m_instance;
        }
    }

    private static UIManager m_instance; // 싱글톤이 할당될 변수

    public Text ammoText; // 탄약 표시용 텍스트
    public Text scoreText; // 점수 표시용 텍스트
    public Text timeText; // 진행 시간 표시용 텍스트(m:ss, 일시정지 중에는 흐르지 않는 게임 시간)
    private int lastShownSeconds = -1; // 마지막으로 표시한 초(같은 초에는 문자열을 다시 만들지 않기 위함)
    public Text waveText; // 적 웨이브 표시용 텍스트
    public GameObject gameoverUI; // 게임 오버시 활성화할 UI
    public Text resultStatsText; // 게임 오버 화면에 표시할 결과 요약(점수/생존시간/도달 웨이브)
    public GameObject pauseUI; // 일시정지 중 활성화할 UI
    public GameObject leaveConfirmUI; // "선택 화면으로 돌아가기" 확인 패널(현재 판이 종료됨을 알림)
    public string introSceneName = "Intro"; // 선택 화면(캐릭터 선택 패널)이 있는 인트로 씬 이름
    public GameObject goalResultUI; // 1차 목표 달성 결과 화면(결과 테스트 모드에서만 사용, 무한 모드 진입 여부를 묻는다)
    public Text goalResultTitle; // 결과 화면 제목
    public Text goalResultStats; // 결과 화면의 점수·생존시간·웨이브
    public Text goalResultQuestion; // 결과 화면의 안내 문구("ENTER ENDLESS MODE?")
    public GameObject[] goalChoiceObjects; // 진입 여부를 고르는 동안 보이는 버튼(ENDLESS / FINISH)
    public GameObject[] goalFinishObjects; // 종료를 고른 뒤 보이는 버튼(RESTART / SELECT)
    public GameObject progressErrorUI; // 진행 불가 오류 시 활성화할 UI(재시작 버튼 포함)
    public Text progressErrorText; // 오류 UI의 안내 문구

    public GameObject goalReachedUI; // 1차 목표(30분 생존) 달성 시 잠시 표시할 알림 UI(없으면 표시 생략)
    public Text goalReachedText; // 알림 UI 안의 문구(없으면 기존 문구 유지)
    public float goalReachedDisplaySeconds = 5f; // 알림 표시 시간(초)

    private int lastScore; // 결과 화면 표시용으로 마지막으로 갱신된 점수를 기억
    private int lastWave; // 결과 화면 표시용으로 마지막으로 갱신된 웨이브를 기억
    private string primaryGoalSnapshot; // 1차 목표 달성 시점의 결과 스냅샷(기록 후 변경하지 않음)

    // 탄약 텍스트 갱신 (remainAmmo가 음수면 무제한 무기, ∞로 표시)
    public void UpdateAmmoText(int magAmmo, int remainAmmo) {
        ammoText.text = magAmmo + " / " + (remainAmmo < 0 ? "∞" : remainAmmo.ToString());
    }

    // 진행 시간 텍스트 갱신(초가 바뀔 때만 문자열을 만든다)
    public void UpdateTimeText(int totalSeconds) {
        if (timeText == null || totalSeconds == lastShownSeconds)
        {
            return;
        }

        lastShownSeconds = totalSeconds;
        timeText.text = totalSeconds / 60 + ":" + (totalSeconds % 60).ToString("00");
    }

    // 점수 텍스트 갱신
    public void UpdateScoreText(int newScore) {
        lastScore = newScore;
        scoreText.text = "Score : " + newScore;
    }

    // 적 웨이브 텍스트 갱신
    // 남은 적 = 살아있는 적 + 동시 상한 때문에 소환 대기 중인 적("Enemy Left : 살아있는 수 + 대기 수")
    public void UpdateWaveText(int waves, int aliveCount, int pendingCount) {
        lastWave = waves;
        waveText.text = "Wave : " + waves + "\nEnemy Left : " + aliveCount + " + " + FormatCount(pendingCount);
    }

    // 준비 시간 안내 텍스트(첫 웨이브가 시작하기 전까지 웨이브 패널에 남은 초를 표시)
    public void UpdatePrepareText(int secondsLeft) {
        waveText.text = "Get Ready : " + secondsLeft + "\nEnemy Left : 0 + 0";
    }

    // 후반에 대기 수가 매우 커져 패널 폭을 넘지 않도록 1만부터 k 단위로 줄여 표시
    private static string FormatCount(int count) {
        return count >= 10000 ? (count / 1000) + "k" : count.ToString();
    }

    // 게임 오버 UI 활성화(활성화하는 순간의 점수/생존시간/웨이브를 결과 텍스트에 채움)
    public void SetActiveGameoverUI(bool active) {
        if (active && resultStatsText != null)
        {
            int totalSeconds = Mathf.FloorToInt(Time.timeSinceLevelLoad);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            resultStatsText.text = "SCORE : " + lastScore
                + "\nSURVIVED : " + minutes + ":" + seconds.ToString("00")
                + "\nWAVE : " + lastWave;

            // 1차 목표를 달성한 판이면 달성 시점 스냅샷을 작은 강조색 한 줄로 함께 표시(결과 패널 높이 안에 맞춤)
            if (!string.IsNullOrEmpty(primaryGoalSnapshot))
            {
                resultStatsText.text += "\n<size=15><color=#EDBD5C>" + primaryGoalSnapshot + "</color></size>";
            }
        }

        gameoverUI.SetActive(active);
    }

    // 1차 목표 달성 시점의 점수/시간/웨이브를 한 번만 기록하고 알림을 잠시 표시
    public void RecordPrimaryGoalSnapshot(int score) {
        // 에디터가 비어 있는 문자열로 복원하는 경우도 "아직 기록 안 됨"으로 취급
        if (!string.IsNullOrEmpty(primaryGoalSnapshot))
        {
            return;
        }

        int totalSeconds = Mathf.FloorToInt(Time.timeSinceLevelLoad);
        string time = totalSeconds / 60 + ":" + (totalSeconds % 60).ToString("00");
        primaryGoalSnapshot = "GOAL CLEARED " + time + "  /  SCORE " + score + "  /  WAVE " + lastWave;

        if (goalReachedUI != null)
        {
            if (goalReachedText != null)
            {
                goalReachedText.text = "BOSS DEFEATED " + time + "\nENDLESS MODE";
            }

            goalReachedUI.SetActive(true);
            // 일시정지 중에는 알림 시간도 멈추도록 스케일된 시간 기준 Invoke 사용
            Invoke(nameof(HideGoalReachedUI), goalReachedDisplaySeconds);
        }
    }

    private void HideGoalReachedUI() {
        goalReachedUI.SetActive(false);
    }

    // 1차 목표 달성 결과 화면 표시: 점수·생존 시간·웨이브와 "무한 모드에 들어갈지" 묻는 안내
    public void ShowGoalResult(int score) {
        if (goalResultUI == null)
        {
            return;
        }

        // 잠깐 뜨는 달성 배너는 결과 화면과 겹치지 않도록 숨긴다
        CancelInvoke(nameof(HideGoalReachedUI));
        if (goalReachedUI != null)
        {
            goalReachedUI.SetActive(false);
        }

        int totalSeconds = Mathf.FloorToInt(Time.timeSinceLevelLoad);
        goalResultTitle.text = "GOAL CLEARED";
        goalResultStats.text = "SCORE : " + score
            + "\nSURVIVED : " + totalSeconds / 60 + ":" + (totalSeconds % 60).ToString("00")
            + "\nWAVE : " + lastWave;
        goalResultQuestion.text = "ENTER ENDLESS MODE?";
        SetObjectsActive(goalChoiceObjects, true);
        SetObjectsActive(goalFinishObjects, false);
        goalResultUI.SetActive(true);
    }

    // 결과 화면 닫기(무한 모드 진입)
    public void HideGoalResult() {
        if (goalResultUI != null)
        {
            goalResultUI.SetActive(false);
        }
    }

    // 종료를 고른 뒤의 결과 화면: 선택 버튼을 숨기고 RESTART / SELECT를 보여 준다
    public void ShowGoalFinished() {
        if (goalResultUI == null)
        {
            return;
        }

        goalResultTitle.text = "RUN COMPLETE";
        goalResultQuestion.text = "";
        SetObjectsActive(goalChoiceObjects, false);
        SetObjectsActive(goalFinishObjects, true);
    }

    private static void SetObjectsActive(GameObject[] objects, bool active) {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                objects[i].SetActive(active);
            }
        }
    }

    // 진행 불가 오류 UI 표시(연결되지 않았으면 로그만 남긴다)
    public void ShowProgressError(string message) {
        Debug.LogError("[UIManager] 진행 오류: " + message);

        if (progressErrorUI == null)
        {
            return;
        }

        if (progressErrorText != null)
        {
            progressErrorText.text = message;
        }

        progressErrorUI.SetActive(true);
    }

    // 일시정지 UI 활성화
    public void SetActivePauseUI(bool active) {
        pauseUI.SetActive(active);

        // 일시정지가 풀리면(ESC 등) 열려 있던 확인 패널도 함께 닫는다
        if (!active && leaveConfirmUI != null)
        {
            leaveConfirmUI.SetActive(false);
        }
    }

    // 일시정지 메뉴의 "선택 화면" 버튼: 진행 중인 판이 종료됨을 확인하는 패널을 연다
    public void ShowLeaveConfirm() {
        leaveConfirmUI.SetActive(true);
    }

    // 확인 패널의 취소 버튼
    public void HideLeaveConfirm() {
        leaveConfirmUI.SetActive(false);
    }

    // 인트로의 캐릭터 선택 패널로 돌아간다(현재 판은 종료됨)
    public void ReturnToCharacterSelect() {
        // 일시정지 중에 이동하는 경우가 없도록 시간 배율을 원복
        Time.timeScale = 1f;
        CharacterSelection.openSelectOnIntro = true;
        SceneManager.LoadScene(introSceneName);
    }

    // 게임 재시작
    public void GameRestart() {
        // 일시정지 중 재시작하는 경우가 없도록 시간 배율을 원복(안전장치)
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}