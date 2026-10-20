using NUnit.Framework;

public class TouchPointerTrackerTests {
    [Test]
    public void Press_SecondFingerIsIgnoredWhileHeld() {
        var tracker = new TouchPointerTracker();
        Assert.IsTrue(tracker.TryPress(0));
        Assert.IsFalse(tracker.TryPress(1));
        Assert.IsTrue(tracker.IsActive);
    }

    [Test]
    public void Release_OnlyByTheSameFinger() {
        var tracker = new TouchPointerTracker();
        tracker.TryPress(3);
        Assert.IsFalse(tracker.TryRelease(4));
        Assert.IsTrue(tracker.IsActive);
        Assert.IsTrue(tracker.TryRelease(3));
        Assert.IsFalse(tracker.IsActive);
    }

    [Test]
    public void Exit_ReleasesWhenAllowed() {
        var tracker = new TouchPointerTracker();
        tracker.TryPress(0);
        Assert.IsTrue(tracker.TryExit(0, true));
        Assert.IsFalse(tracker.IsActive);
    }

    [Test]
    public void Exit_KeepsHoldWhenNotAllowed_FireButtonCase() {
        var tracker = new TouchPointerTracker();
        tracker.TryPress(0);
        Assert.IsFalse(tracker.TryExit(0, false));
        Assert.IsTrue(tracker.IsActive);
        Assert.IsTrue(tracker.TryRelease(0)); // 뗄 때 해제
    }

    [Test]
    public void Exit_FromOtherFingerIsIgnored() {
        var tracker = new TouchPointerTracker();
        tracker.TryPress(0);
        Assert.IsFalse(tracker.TryExit(1, true));
        Assert.IsTrue(tracker.IsActive);
    }

    [Test]
    public void Clear_ResetsAndAllowsNewPress() {
        var tracker = new TouchPointerTracker();
        tracker.TryPress(0);
        tracker.Clear();
        Assert.IsFalse(tracker.IsActive);
        Assert.IsTrue(tracker.TryPress(5));
    }

    [Test]
    public void PointerIdZeroAndNegativeAreValid() {
        var tracker = new TouchPointerTracker();
        Assert.IsTrue(tracker.TryPress(-1)); // 마우스 왼쪽 버튼의 포인터 ID는 -1
        Assert.IsTrue(tracker.TryRelease(-1));
    }
}
