using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리자 관련 코드

// 인트로(타이틀) 화면의 게임 시작·종료 버튼 처리
// (설정 버튼은 씬에서 GameSettingsKit.SettingsPanel.Open에 직접 연결)
public class IntroMenu : MonoBehaviour {
    public string gameSceneName = "UrbanSurvival"; // START로 불러올 인게임 씬 이름

    // 게임 시작
    public void StartGame() {
        // 일시정지 상태로 넘어오는 경우가 없도록 시간 배율 원복(안전장치)
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    // 게임 종료(에디터에서는 Play Mode 종료)
    public void QuitGame() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
