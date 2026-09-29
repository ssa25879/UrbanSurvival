using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리자 관련 코드

// 인트로(타이틀) 화면의 게임 시작·종료 버튼 처리
// (설정 버튼은 씬에서 GameSettingsKit.SettingsPanel.Open에 직접 연결)
public class IntroMenu : MonoBehaviour {
    public string gameSceneName = "UrbanSurvival"; // START로 불러올 인게임 씬 이름
    public CharacterSelectMenu characterSelect; // 인게임에서 선택 화면으로 돌아왔을 때 바로 열 캐릭터 선택 패널

    private void Start() {
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

    // [임시] 결과 화면 테스트 씬으로 이동(게임 시작 30초 후 결과 화면 확인용).
    // 확인이 끝나면 이 메서드, resultTestSceneName, 인트로 씬의 TEST 버튼, 빌드 목록의 테스트 씬을 함께 지운다
    public string resultTestSceneName = "UrbanSurvival_ResultTest";

    public void StartResultTest() {
        Time.timeScale = 1f;
        SceneManager.LoadScene(resultTestSceneName);
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
