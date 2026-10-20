using NUnit.Framework;
using UnityEngine;

public class SafeAreaMathTests {
    private const float Tolerance = 0.0001f;

    [Test]
    public void FullScreen_IsUnitRect() {
        SafeAreaMath.ToAnchors(new Rect(0, 0, 2400, 1080), 2400, 1080, out Vector2 min, out Vector2 max);
        Assert.AreEqual(0f, min.x, Tolerance);
        Assert.AreEqual(0f, min.y, Tolerance);
        Assert.AreEqual(1f, max.x, Tolerance);
        Assert.AreEqual(1f, max.y, Tolerance);
    }

    [Test]
    public void NotchOnLeftAndRound_CutsSideAndTop() {
        // 가로 화면: 왼쪽 노치 100px, 오른쪽 둥근 모서리 60px, 위 20px, 아래 인디케이터 40px
        SafeAreaMath.ToAnchors(new Rect(100, 40, 2240, 1020), 2400, 1080, out Vector2 min, out Vector2 max);
        Assert.AreEqual(100f / 2400f, min.x, Tolerance);
        Assert.AreEqual(40f / 1080f, min.y, Tolerance);
        Assert.AreEqual(2340f / 2400f, max.x, Tolerance);
        Assert.AreEqual(1060f / 1080f, max.y, Tolerance);
    }

    [Test]
    public void ZeroScreen_FallsBackToFullScreen() {
        SafeAreaMath.ToAnchors(new Rect(0, 0, 0, 0), 0, 0, out Vector2 min, out Vector2 max);
        Assert.AreEqual(Vector2.zero, min);
        Assert.AreEqual(Vector2.one, max);
    }

    [Test]
    public void OutOfBoundsSafeArea_IsClampedToUnit() {
        SafeAreaMath.ToAnchors(new Rect(-50, -10, 3000, 1200), 2400, 1080, out Vector2 min, out Vector2 max);
        Assert.AreEqual(0f, min.x, Tolerance);
        Assert.AreEqual(0f, min.y, Tolerance);
        Assert.AreEqual(1f, max.x, Tolerance);
        Assert.AreEqual(1f, max.y, Tolerance);
    }
}
