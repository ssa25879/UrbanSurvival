using UnityEngine;

// 쌍둥이 스틱 모드: 조준 스틱이 데드존 밖에 있는 동안 발사 유지, 데드존 밖으로 처음 나간 프레임에 FireDown
// 입력은 JoystickMath.Evaluate의 출력(데드존 이하는 0)이다
public sealed class TwinStickFireTracker {
    private const float PullThresholdSqr = 0.0001f;

    private bool wasHeld;

    public bool Held { get; private set; }
    public bool Down { get; private set; }

    public void Update(Vector2 stick) {
        bool pulled = stick.sqrMagnitude > PullThresholdSqr;
        Down = pulled && !wasHeld;
        Held = pulled;
        wasHeld = pulled;
    }

    public void Reset() {
        Held = false;
        Down = false;
        wasHeld = false;
    }
}
