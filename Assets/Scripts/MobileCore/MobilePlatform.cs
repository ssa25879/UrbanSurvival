using UnityEngine;

// 모바일 입력·터치 UI 사용 여부 판단. 에디터에서만 강제 플래그를 허용하고 실제 빌드의 판정은 덮어쓰지 않는다
public static class MobilePlatform {
#if UNITY_EDITOR
    private const string ForceKey = "UrbanSurvival.ForceMobileInEditor";

    public static bool ForceMobileInEditor {
        get { return UnityEditor.EditorPrefs.GetBool(ForceKey, false); }
        set { UnityEditor.EditorPrefs.SetBool(ForceKey, value); }
    }

    public static bool IsMobile {
        get { return ForceMobileInEditor || Application.isMobilePlatform; }
    }
#else
    public static bool IsMobile {
        get { return Application.isMobilePlatform; }
    }
#endif
}
