using NUnit.Framework;
using UnityEngine;

public class JoystickMathTests {
    private const float Tolerance = 0.0001f;

    [Test]
    public void NoInput_ReturnsZero() {
        Assert.AreEqual(Vector2.zero, JoystickMath.Evaluate(Vector2.zero, 100f, 0.2f));
    }

    [Test]
    public void InsideDeadZone_IsIgnored() {
        Vector2 result = JoystickMath.Evaluate(new Vector2(15f, 0f), 100f, 0.2f);
        Assert.AreEqual(Vector2.zero, result);
    }

    [Test]
    public void AtMaxRadius_ReturnsUnitVector() {
        Vector2 result = JoystickMath.Evaluate(new Vector2(0f, 100f), 100f, 0.2f);
        Assert.AreEqual(0f, result.x, Tolerance);
        Assert.AreEqual(1f, result.y, Tolerance);
    }

    [Test]
    public void BeyondRadius_IsClampedToUnit() {
        Vector2 result = JoystickMath.Evaluate(new Vector2(300f, 400f), 100f, 0.2f);
        Assert.AreEqual(1f, result.magnitude, Tolerance);
    }

    [Test]
    public void Diagonal_MaxInput_MagnitudeNotAboveOne() {
        Vector2 result = JoystickMath.Evaluate(new Vector2(100f, 100f), 100f, 0.2f);
        Assert.LessOrEqual(result.magnitude, 1f + Tolerance);
        Assert.AreEqual(result.x, result.y, Tolerance);
    }

    [Test]
    public void OutsideDeadZone_KeepsDirection() {
        Vector2 result = JoystickMath.Evaluate(new Vector2(60f, 0f), 100f, 0.2f);
        Assert.Greater(result.x, 0f);
        Assert.AreEqual(0f, result.y, Tolerance);
        // (0.6 - 0.2) / (1 - 0.2) = 0.5
        Assert.AreEqual(0.5f, result.magnitude, Tolerance);
    }

    [Test]
    public void InvalidRadius_ReturnsZeroWithoutNaN() {
        Vector2 result = JoystickMath.Evaluate(new Vector2(10f, 10f), 0f, 0.2f);
        Assert.AreEqual(Vector2.zero, result);
    }

    [Test]
    public void DeadZoneAtOrAboveOne_IsClampedAndNeverDividesByZero() {
        Vector2 result = JoystickMath.Evaluate(new Vector2(100f, 0f), 100f, 1f);
        Assert.IsFalse(float.IsNaN(result.x) || float.IsNaN(result.y));
        Assert.LessOrEqual(result.magnitude, 1f + Tolerance);
    }

    [Test]
    public void ToWorld_TopDownCameraLookingNorth_MapsStickUpToForward() {
        Vector3 forward = JoystickMath.ToWorldDirection(new Vector2(0f, 1f), Vector3.forward, Vector3.up);
        Vector3 right = JoystickMath.ToWorldDirection(new Vector2(1f, 0f), Vector3.forward, Vector3.up);
        Assert.AreEqual(0f, forward.x, Tolerance);
        Assert.AreEqual(1f, forward.z, Tolerance);
        Assert.AreEqual(1f, right.x, Tolerance);
        Assert.AreEqual(0f, right.z, Tolerance);
    }

    [Test]
    public void ToWorld_VerticalCamera_UsesCameraUpAsForward() {
        // 수직 탑뷰 카메라: forward가 아래를 향해 평면 투영이 0이면 camera.up을 화면 위쪽으로 쓴다
        Vector3 result = JoystickMath.ToWorldDirection(new Vector2(0f, 1f), Vector3.down, Vector3.forward);
        Assert.AreEqual(1f, result.z, Tolerance);
        Assert.AreEqual(0f, result.y, Tolerance);
    }

    [Test]
    public void ToWorld_ZeroStick_ReturnsZeroVector() {
        Assert.AreEqual(Vector3.zero, JoystickMath.ToWorldDirection(Vector2.zero, Vector3.forward, Vector3.up));
    }
}
