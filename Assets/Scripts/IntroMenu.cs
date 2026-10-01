using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리자 관련 코드

// 인트로(타이틀) 화면의 게임 시작·종료 버튼 처리
// (설정 버튼은 씬에서 GameSettingsKit.SettingsPanel.Open에 직접 연결)
public class IntroMenu : MonoBehaviour {
    public string gameSceneName = "UrbanSurvival"; // START로 불러올 인게임 씬 이름
    public CharacterSelectMenu characterSelect; // 인게임에서 선택 화면으로 돌아왔을 때 바로 열 캐릭터 선택 패널
    public PracticeSelectMenu practiceSelect; // 연습 씬에서 돌아왔을 때 바로 열 연습 패널(2026-10-01)

    private void Start() {
        // 연습 씬의 SELECT로 온 경우 연습 패널을 연다
        if (CharacterSelection.openPracticeOnIntro)
        {
            CharacterSelection.openPracticeOnIntro = false;
            if (practiceSelect != null)
            {
                practiceSelect.Open();
            }
        }

        // 인게임의 "선택 화면으로 돌아가기"로 온 경우 메인 메뉴를 거치지 않고 캐릭터 선택 패널을 연다
        if (CharacterSelection.openSelectOnIntro)
        {
            CharacterSelection.openSelectOnIntro = false;
            if (characterSelect != null)
            {
                characterSelect.Open();
            }
        }
    }

    // 게임 시작
    public void StartGame() {
        // 일시정지 상태로 넘어오는 경우가 없도록 시간 배율 원복(안전장치)
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    // 지정한 씬을 불러온다(연습 씬 입장용)
    public void LoadScene(string sceneName) {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
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
