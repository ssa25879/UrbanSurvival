using System;
using UnityEngine;

public enum MobileAimMode {
    AutoAim = 0,   // 가장 가까운 적 자동 조준 + 발사 버튼(기본값)
    TwinStick = 1  // 오른쪽 스틱으로 조준·발사
}

// 모바일 조준 모드 저장·복원(PlayerPrefs). 설정 창 연결은 모듈 밖의 MobileAimModeSettingsBinder가 맡는다
public static class MobileAimSettings {
    public const string PrefsKey = "MobileAimMode";

    public static event Action<MobileAimMode> Changed;

    public static MobileAimMode Mode {
        get {
            int stored = PlayerPrefs.GetInt(PrefsKey, (int)MobileAimMode.AutoAim);
            return Enum.IsDefined(typeof(MobileAimMode), stored) ? (MobileAimMode)stored : MobileAimMode.AutoAim;
        }
        set {
            if (Mode == value)
            {
                return;
            }

            PlayerPrefs.SetInt(PrefsKey, (int)value);
            PlayerPrefs.Save();
            Changed?.Invoke(value);
        }
    }
}
