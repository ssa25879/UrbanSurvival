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

    private int lastScore; // 결과 화면 표시용으로 마지막으로 갱신된 점수를 기억
    private int lastWave; // 결과 화면 표시용으로 마지막으로 갱신된 웨이브를 기억

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
    public void UpdateWaveText(int waves, int count) {
        lastWave = waves;
        waveText.text = "Wave : " + waves + "\nEnemy Left : " + count;
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
        }

        gameoverUI.SetActive(active);
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