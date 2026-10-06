using NUnit.Framework;
using UnityEngine;

public class TwinStickFireTrackerTests {
    private static Vector2 Stick(float x, float y) {
        return JoystickMath.Evaluate(new Vector2(x, y), 100f, 0.25f);
    }

    [Test]
    public void InsideDeadZone_DoesNotFire() {
        var tracker = new TwinStickFireTracker();
        tracker.Update(Stick(10f, 0f));
        Assert.IsFalse(tracker.Held);
        Assert.IsFalse(tracker.Down);
    }

    [Test]
    public void CrossingDeadZone_ProducesDownOnce_ThenHeld() {
        var tracker = new TwinStickFireTracker();
        tracker.Update(Stick(80f, 0f));
        Assert.IsTrue(tracker.Held);
        Assert.IsTrue(tracker.Down);

        tracker.Update(Stick(80f, 0f));
        Assert.IsTrue(tracker.Held);
        Assert.IsFalse(tracker.Down, "유지 중에는 새 FireDown이 없어야 한다");
    }

    [Test]
    public void ReleaseToCenter_StopsFire_AndNextPullIsNewDown() {
        var tracker = new TwinStickFireTracker();
        tracker.Update(Stick(80f, 0f));
        tracker.Update(Vector2.zero);
        Assert.IsFalse(tracker.Held);
        Assert.IsFalse(tracker.Down);

        tracker.Update(Stick(0f, 80f));
        Assert.IsTrue(tracker.Down);
    }

    [Test]
    public void ChangingDirectionWhileHeld_DoesNotCreateNewDown() {
        var tracker = new TwinStickFireTracker();
        tracker.Update(Stick(80f, 0f));
        tracker.Update(Stick(0f, 80f));
        Assert.IsTrue(tracker.Held);
        Assert.IsFalse(tracker.Down);
    }

    [Test]
    public void Reset_ClearsState_SoHeldStickIsNewPull() {
        var tracker = new TwinStickFireTracker();
        tracker.Update(Stick(80f, 0f));
        tracker.Reset();
        Assert.IsFalse(tracker.Held);
        Assert.IsFalse(tracker.Down);
    }
}
