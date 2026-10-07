using System;
using UnityEngine;

namespace GameSettingsKit
{
    // 저장·적용 대상이 되는 설정값 묶음. 필드를 추가할 때는 SettingsStore의 Load/Save/Apply도 함께 갱신한다.
    [Serializable]
    public struct SettingsData
    {
        [Range(0f, 1f)] public float masterVolume;
        [Range(0f, 1f)] public float musicVolume;
        [Range(0f, 1f)] public float sfxVolume;
        public bool fullscreen;
        public int resolutionWidth; // 0 이하이면 해상도는 바꾸지 않고 전체 화면 여부만 적용
        public int resolutionHeight;
        public int qualityLevel; // 범위 밖(예: -1)이면 품질 단계를 바꾸지 않음
        public bool vSync;

        // 현재 실행 환경 기준 기본값(SettingsStore.Defaults가 게임 시작 시 한 번만 호출)
        public static SettingsData CreateDefault()
        {
            return new SettingsData
            {
                masterVolume = 1f,
                musicVolume = 0.8f,
                sfxVolume = 1f,
                fullscreen = Screen.fullScreen,
                resolutionWidth = Screen.width,
                resolutionHeight = Screen.height,
                qualityLevel = QualitySettings.GetQualityLevel(),
                vSync = QualitySettings.vSyncCount > 0
            };
        }
    }
}
