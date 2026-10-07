using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 모바일에서는 게임 종료(QUIT) 버튼을 숨긴다(Android는 앱 종료 버튼을 두지 않는 것이 관례).
// 씬의 버튼 중 onClick에 QuitGame이 연결된 것을 찾아 끈다(씬 파일을 수정하지 않는다)
public static class MobileQuitHider {
    private const string QuitMethodName = "QuitGame";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap() {
        SceneManager.sceneLoaded += (scene, mode) => HideAll();
        HideAll();
    }

    private static void HideAll() {
        if (!MobilePlatform.IsMobile)
        {
            return;
        }

        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentMethodName(i) == QuitMethodName)
                {
                    button.gameObject.SetActive(false);
                    break;
                }
            }
        }
    }
}
