using System;

// 화면에 표시하는 무기 이름. 씬의 총 오브젝트 이름(AK)은 바꾸지 않고 표기만 바꾼다(2026-10-06: AK를 AR로 표기)
public static class WeaponDisplayName {
    private static readonly string[] SlotLabels = { "PISTOL", "AR", "SMG", "SG" };

    // HUD 무기 패널 등에 쓰는 이름: AK는 AR로, 나머지는 그대로
    public static string Get(string objectName) {
        if (objectName == null)
        {
            return string.Empty;
        }

        return string.Equals(objectName, "AK", StringComparison.OrdinalIgnoreCase) ? "AR" : objectName;
    }

    // 모바일 무기 슬롯 버튼의 축약 표기(슬롯 0~3 = 권총, 소총, SMG, 산탄총). 범위 밖이면 빈 문자열
    public static string SlotShortLabel(int slotIndex) {
        return slotIndex >= 0 && slotIndex < SlotLabels.Length ? SlotLabels[slotIndex] : string.Empty;
    }
}
