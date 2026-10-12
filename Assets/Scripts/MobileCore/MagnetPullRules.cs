// 자석 버프가 끌어와도 되는 아이템인지 판단하는 순수 로직(2026-10-12).
// 기획서 13장: 체력·탄약이 가득 차면 보급을 소비하지 않는다. 자석이 쓸모없는 상자를 끌어와 없애지 않도록 막는다
public static class MagnetPullRules {
    // 회복 상자: 현재 체력이 최대 미만일 때만
    public static bool ShouldPullHealthPack(float health, float maxHealth) {
        return health < maxHealth;
    }

    // 탄약 상자: 예비탄이 무제한(상한 음수)이 아니고 상한 미만일 때만
    public static bool ShouldPullAmmoPack(int reserveAmmo, int reserveCap) {
        return reserveCap >= 0 && reserveAmmo < reserveCap;
    }
}
