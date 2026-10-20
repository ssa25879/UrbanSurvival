using NUnit.Framework;

public class SpawnBudgetTests {
    [Test]
    public void Take_LimitedByPerFrame() {
        Assert.AreEqual(12, SpawnBudget.Take(300, 0, 500, 12));
    }

    [Test]
    public void Take_LimitedByPending() {
        Assert.AreEqual(5, SpawnBudget.Take(5, 0, 500, 12));
    }

    [Test]
    public void Take_LimitedByRoomUnderCap() {
        Assert.AreEqual(3, SpawnBudget.Take(300, 497, 500, 12));
    }

    [Test]
    public void Take_AtOrOverCapIsZero() {
        Assert.AreEqual(0, SpawnBudget.Take(300, 500, 500, 12));
        Assert.AreEqual(0, SpawnBudget.Take(300, 520, 500, 12));
    }

    [Test]
    public void Take_NothingPendingOrNoBudgetIsZero() {
        Assert.AreEqual(0, SpawnBudget.Take(0, 0, 500, 12));
        Assert.AreEqual(0, SpawnBudget.Take(-3, 0, 500, 12));
        Assert.AreEqual(0, SpawnBudget.Take(10, 0, 500, 0));
        Assert.AreEqual(0, SpawnBudget.Take(10, 0, 500, -1));
    }

    [Test]
    public void FailureRule_RecentFailureIsContinuation() {
        Assert.IsTrue(SpawnFailureRule.IsContinuation(10f, 9f, 3f));
        Assert.IsTrue(SpawnFailureRule.IsContinuation(12f, 9f, 3f)); // 경계 포함
    }

    [Test]
    public void FailureRule_OldFailureStartsNewSpan() {
        Assert.IsFalse(SpawnFailureRule.IsContinuation(30f, 9f, 3f));
    }

    [Test]
    public void FailureRule_NoPreviousFailureIsNotContinuation() {
        Assert.IsFalse(SpawnFailureRule.IsContinuation(5f, -1f, 3f));
    }
}
