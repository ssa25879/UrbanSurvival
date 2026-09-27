using UnityEngine;

namespace GameSettingsKit
{
    // 설정 창 자동 생성(에디터 메뉴) 시 사용할 외형. 프로젝트마다 에셋을 하나 만들어 폰트·스프라이트·색만 바꾼다.
    [CreateAssetMenu(menuName = "Game Settings Kit/Settings Panel Theme", fileName = "SettingsPanelTheme")]
    public class SettingsPanelTheme : ScriptableObject
    {
        [Header("리소스(비우면 Unity 기본 UI 리소스 사용)")]
        public Font font;
        public Sprite panelSprite; // 창·버튼 배경(9-slice 권장)
        public Sprite accentLineSprite; // 창 상단 강조선(선택)
        public Sprite checkmarkSprite; // 토글 체크 표시(비우면 강조색으로 채운 사각형)

        [Header("색")]
        // Linear 색 공간 프로젝트에서는 반투명 UI 뒤 내용이 Gamma보다 훨씬 밝게 비치므로(알파 0.96이어도 약 20%로 보임)
        // 창 배경은 불투명(알파 1)으로 두고, 딤은 0.8 이상을 권장
        public Color dimColor = new Color(0f, 0f, 0f, 0.85f);
        public Color panelColor = new Color(0.07f, 0.08f, 0.09f, 1f);
        public Color accentColor = new Color(0.93f, 0.74f, 0.36f, 1f);
        public Color textColor = new Color(0.95f, 0.95f, 0.93f, 1f);
        public Color subTextColor = new Color(0.62f, 0.66f, 0.70f, 1f);
        public Color controlColor = new Color(0.20f, 0.22f, 0.25f, 1f);
        public Color primaryButtonTextColor = new Color(0.08f, 0.08f, 0.09f, 1f);

        [Header("크기")]
        public Vector2 windowSize = new Vector2(560f, 470f);
        public int titleFontSize = 36;
        public int labelFontSize = 18;
        public float rowHeight = 44f;

        [Header("문구")]
        public string title = "SETTINGS";
        public string masterVolumeLabel = "MASTER VOLUME";
        public string musicVolumeLabel = "MUSIC";
        public string sfxVolumeLabel = "SFX";
        public string fullscreenLabel = "FULLSCREEN";
        public string resolutionLabel = "RESOLUTION";
        public string qualityLabel = "GRAPHICS";
        public string vSyncLabel = "VSYNC";
        public string resetLabel = "RESET";
        public string closeLabel = "CLOSE";
    }
}
