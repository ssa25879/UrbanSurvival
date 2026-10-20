// 한 프레임에 소환할 좀비 수 계산(순수 로직).
// 대기열이 수백 마리여도 프레임 스파이크가 생기지 않도록 프레임당 상한을 두고, 동시 생존 상한(cap)도 넘지 않는다
public static class SpawnBudget {
    public static int Take(int pending, int alive, int cap, int perFrame) {
        if (pending <= 0 || perFrame <= 0)
        {
            return 0;
        }

        int room = cap - alive;
        if (room <= 0)
        {
            return 0;
        }

        return System.Math.Min(System.Math.Min(pending, room), perFrame);
    }
}

// 소환 실패가 "연속"인지 판단하는 규칙(순수 로직).
// 마지막 실패로부터 gapSeconds 넘게 지나 다시 실패하면 연속이 아니라 새 실패 구간의 시작으로 본다.
// (연습 씬처럼 한동안 소환 시도가 없다가 한 번 실패했을 때, 오래된 시작 시각 때문에 곧바로 오류가 나는 문제 방지)
public static class SpawnFailureRule {
    public static bool IsContinuation(float now, float lastFailureTime, float gapSeconds) {
        return lastFailureTime >= 0f && now - lastFailureTime <= gapSeconds;
    }
}
