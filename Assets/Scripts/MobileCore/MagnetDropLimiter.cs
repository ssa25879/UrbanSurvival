using System.Collections.Generic;

// 강화 적 처치로 떨어지는 자석 아이템을 "강화 웨이브당 최대 1개"로 제한한다(순수 로직, 2026-10-08 사양).
// 웨이브 번호는 소환 시점이 아니라 그 강화 적이 속한 강화 웨이브 번호다(대기열에서 늦게 소환돼도 같은 웨이브로 센다)
public class MagnetDropLimiter {
    private readonly HashSet<int> claimedWaves = new HashSet<int>();

    // 그 웨이브의 자석 지급권을 처음 요청하면 true(이후 같은 웨이브는 false)
    public bool TryClaim(int wave) {
        return claimedWaves.Add(wave);
    }

    // 이미 지급된 웨이브인지(지급권을 쓰지 않고 확인만 한다)
    public bool HasClaimed(int wave) {
        return claimedWaves.Contains(wave);
    }
}
