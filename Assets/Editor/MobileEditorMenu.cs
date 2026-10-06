using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

// 에디터에서 모바일 입력·터치 UI를 강제로 켜고 끄는 메뉴(실제 빌드의 모바일 판정에는 영향이 없다)
// 모바일 포팅의 Android Player Settings 적용 메뉴도 여기에 둔다
public static class MobileEditorMenu
{
    // 모바일 포팅의 Android Player Settings를 한 번에 적용한다(재현 가능하도록 코드로 둔다)
    [MenuItem("Urban Survival/Mobile/Apply Android Settings")]
    public static void ApplyAndroidSettings()
    {
        PlayerSettings.companyName = "YWS";
        PlayerSettings.productName = "Urban Survival";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.yws.urbansurvival");

        // 가로 화면만 허용
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
        EditorUserBuildSettings.buildAppBundle = true;

        // 프로젝트가 URP 호환 모드(Render Graph 끔)이므로 Android 빌드에도 같은 정의가 있어야 빌드가 거부되지 않는다
        string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
        if (!defines.Contains("URP_COMPATIBILITY_MODE"))
        {
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android,
                defines + (defines.Length > 0 ? ";" : "") + "URP_COMPATIBILITY_MODE");
        }

        Debug.Log("[Mobile] Android 설정 적용: " + PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
    }

    private const string MenuPath = "Urban Survival/Mobile/Force Mobile Input In Editor";

    // 앱 아이콘을 Android 아이콘 슬롯(적응형·라운드·레거시)에 적용한다.
    // 적응형은 가장자리가 마스크로 잘려서 전체 그림(군인 중심 크롭)을 배경 레이어에 두고 전경 레이어는 투명으로 둔다
    [MenuItem("Urban Survival/Mobile/Apply Android Icons")]
    public static void ApplyAndroidIcons()
    {
        Texture2D art = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Images/Android/AppIcon_1024.png");
        Texture2D empty =
            AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Images/Android/AppIcon_Foreground_Empty.png");
        if (art == null || empty == null)
        {
            Debug.LogError(
                "[Mobile] 아이콘 이미지를 찾을 수 없습니다: Assets/Images/Android/AppIcon_1024.png, AppIcon_Foreground_Empty.png");
            return;
        }

        NamedBuildTarget android = NamedBuildTarget.Android;
        foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(android))
        {
            bool adaptive = kind.ToString().StartsWith("Adaptive");
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(android, kind);
            foreach (PlatformIcon icon in icons)
            {
                icon.SetTextures(adaptive ? new[] { art, empty } : new[] { art });
            }

            PlayerSettings.SetPlatformIcons(android, kind, icons);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Mobile] Android 아이콘 적용 완료");
    }

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        MobilePlatform.ForceMobileInEditor = !MobilePlatform.ForceMobileInEditor;
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, MobilePlatform.ForceMobileInEditor);
        return true;
    }
}