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
    public Text waveText; // 적 웨이브 표시용 텍스트
    public GameObject gameoverUI; // 게임 오버시 활성화할 UI
    public Text resultStatsText; // 게임 오버 화면에 표시할 결과 요약(점수/생존시간/도달 웨이브)
    public GameObject pauseUI; // 일시정지 중 활성화할 UI

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
            if (primaryGoalSnapshot != null)
            {
                resultStatsText.text += "\n<size=15><color=#EDBD5C>" + primaryGoalSnapshot + "</color></size>";
            }
        }

        gameoverUI.SetActive(active);
    }

    // 1차 목표 달성 시점의 점수/시간/웨이브를 한 번만 기록하고 알림을 잠시 표시
    public void RecordPrimaryGoalSnapshot(int score) {
        if (primaryGoalSnapshot != null)
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
                goalReachedText.text = "SURVIVED " + time + "\nENDLESS MODE";
            }

            goalReachedUI.SetActive(true);
            // 일시정지 중에는 알림 시간도 멈추도록 스케일된 시간 기준 Invoke 사용
            Invoke(nameof(HideGoalReachedUI), goalReachedDisplaySeconds);
        }
    }

    private void HideGoalReachedUI() {
        goalReachedUI.SetActive(false);
    }

    // 일시정지 UI 활성화
    public void SetActivePauseUI(bool active) {
        pauseUI.SetActive(active);
    }

    // 게임 재시작
    public void GameRestart() {
        // 일시정지 중 재시작하는 경우가 없도록 시간 배율을 원복(안전장치)
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}