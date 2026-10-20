// 터치 버튼이 어느 손가락(포인터 ID)을 잡고 있는지 추적하는 순수 로직.
// 한 번에 한 손가락만 잡고, 그 손가락의 뗌(그리고 설정에 따라 이탈)에서만 해제한다
public sealed class TouchPointerTracker {
    private const int None = int.MinValue;
    private int activePointerId = None;

    public bool IsActive => activePointerId != None;

    // 아직 잡은 손가락이 없을 때만 잡는다
    public bool TryPress(int pointerId) {
        if (activePointerId != None)
        {
            return false;
        }

        activePointerId = pointerId;
        return true;
    }

    // 잡고 있던 그 손가락이 떼어졌을 때만 해제한다
    public bool TryRelease(int pointerId) {
        if (activePointerId != pointerId)
        {
            return false;
        }

        activePointerId = None;
        return true;
    }

    // 손가락이 버튼 영역 밖으로 나갔을 때: releaseOnExit가 true일 때만 해제(발사 버튼은 false로 둬 계속 누른 상태 유지)
    public bool TryExit(int pointerId, bool releaseOnExit) {
        return releaseOnExit && TryRelease(pointerId);
    }

    public void Clear() {
        activePointerId = None;
    }
}
