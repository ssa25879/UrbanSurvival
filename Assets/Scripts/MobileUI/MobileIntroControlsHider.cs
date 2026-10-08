using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 모바일에서는 인트로(캐릭터 선택) 화면의 PC 조작 안내(WASD·마우스·키 문구)를 숨긴다. 씬 파일은 수정하지 않는다
public static class MobileIntroControlsHider {
    private const string IntroSceneName = "Intro";
    private const string ControlsObjectName = "Controls";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap() {
        SceneManager.sceneLoaded += (scene, mode) => Hide(scene);
        Hide(SceneManager.GetActiveScene());
    }

    private static void Hide(Scene scene) {
        if (!MobilePlatform.IsMobile || scene.name != IntroSceneName)
        {
            return;
        }

        foreach (Text text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (text.gameObject.scene == scene && text.name == ControlsObjectName)
            {
                text.gameObject.SetActive(false);
            }
        }
    }
}
