using System;
using UnityEngine;

public enum MobileAimMode {
    AutoAim = 0,   // 가장 가까운 적 자동 조준 + 발사 버튼(기본값)
    TwinStick = 1  // 오른쪽 스틱으로 조준·발사
}

// 모바일 조준 모드 저장·복원(PlayerPrefs). 설정 창 연결은 모듈 밖의 MobileAimModeSettingsBinder가 맡는다
// 프레임마다 읽히므로(PlayerInput) 값을 캐시하고, 저장소에서는 처음 읽을 때·Reload() 때·쓸 때만 접근한다(2026-10-20)
public static class MobileAimSettings {
    public const string PrefsKey = "MobileAimMode";

    public static event Action<MobileAimMode> Changed;

    private static MobileAimMode cachedMode;
    private static bool hasCached;

    // 도메인 리로드를 끈 Enter Play Mode에서도 이전 플레이의 캐시가 남지 않게 한다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() {
        hasCached = false;
    }

    // 저장소(PlayerPrefs)가 밖에서 바뀌었을 때(설정 초기화 등) 캐시를 버리고 다음 읽기에서 다시 읽게 한다
    public static void Reload() {
        hasCached = false;
    }

    public static MobileAimMode Mode {
        get {
            if (!hasCached)
            {
                int stored = PlayerPrefs.GetInt(PrefsKey, (int)MobileAimMode.AutoAim);
                cachedMode = Enum.IsDefined(typeof(MobileAimMode), stored) ? (MobileAimMode)stored : MobileAimMode.AutoAim;
                hasCached = true;
            }

            return cachedMode;
        }
        set {
            if (Mode == value)
            {
                return;
            }

            PlayerPrefs.SetInt(PrefsKey, (int)value);
            PlayerPrefs.Save();
            cachedMode = value;
            hasCached = true;
            Changed?.Invoke(value);
        }
    }
}
