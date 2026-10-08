// 자석 아이템 버프의 남은 시간(순수 로직). 획득하면 duration으로 초기화(누적 없음), 시간은 Tick으로만 줄어든다(2026-10-08 사양)
public class MagnetBuffTimer {
    public float Remaining { get; private set; }

    public bool IsActive {
        get { return Remaining > 0f; }
    }

    // 버프 시작 또는 재획득: 남은 시간을 duration으로 되돌린다(더하지 않는다)
    public void Refresh(float duration) {
        Remaining = duration > 0f ? duration : 0f;
    }

    // 경과 시간만큼 줄인다. 일시정지 중에는 deltaTime이 0이므로 멈춘다
    public void Tick(float deltaTime) {
        if (Remaining <= 0f || deltaTime <= 0f)
        {
            return;
        }

        Remaining -= deltaTime;
        if (Remaining < 0f)
        {
            Remaining = 0f;
        }
    }

    // 사망·재시작 시 버프를 끝낸다
    public void Clear() {
        Remaining = 0f;
    }
}
