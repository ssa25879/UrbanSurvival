using UnityEditor;

// 에디터에서 모바일 입력·터치 UI를 강제로 켜고 끄는 메뉴(실제 빌드의 모바일 판정에는 영향이 없다)
public static class MobileEditorMenu {
    private const string MenuPath = "Urban Survival/Mobile/Force Mobile Input In Editor";

    [MenuItem(MenuPath)]
    private static void Toggle() {
        MobilePlatform.ForceMobileInEditor = !MobilePlatform.ForceMobileInEditor;
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate() {
        Menu.SetChecked(MenuPath, MobilePlatform.ForceMobileInEditor);
        return true;
    }
}
