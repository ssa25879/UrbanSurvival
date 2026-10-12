using NUnit.Framework;

public class MagnetLogicTests {
    private const float Tolerance = 0.0001f;

    [Test]
    public void Timer_StartsInactive() {
        var timer = new MagnetBuffTimer();
        Assert.IsFalse(timer.IsActive);
        Assert.AreEqual(0f, timer.Remaining, Tolerance);
    }

    [Test]
    public void Timer_RefreshStartsBuffAndTickReducesIt() {
        var timer = new MagnetBuffTimer();
        timer.Refresh(10f);
        Assert.IsTrue(timer.IsActive);

        timer.Tick(4f);
        Assert.AreEqual(6f, timer.Remaining, Tolerance);
    }

    [Test]
    public void Timer_ExpiresAfterDuration() {
        var timer = new MagnetBuffTimer();
        timer.Refresh(10f);
        timer.Tick(10f);
        Assert.IsFalse(timer.IsActive);

        timer.Tick(5f);
        Assert.AreEqual(0f, timer.Remaining, Tolerance, "음수로 내려가지 않는다");
    }

    [Test]
    public void Timer_RefreshAgainResetsToFullDuration_NotAccumulate() {
        var timer = new MagnetBuffTimer();
        timer.Refresh(10f);
        timer.Tick(7f);
        timer.Refresh(10f);
        Assert.AreEqual(10f, timer.Remaining, Tolerance);
    }

    [Test]
    public void Timer_ZeroDeltaTimeKeepsRemaining_ForPause() {
        var timer = new MagnetBuffTimer();
        timer.Refresh(10f);
        timer.Tick(0f);
        Assert.AreEqual(10f, timer.Remaining, Tolerance);
    }

    [Test]
    public void Timer_ClearEndsBuff() {
        var timer = new MagnetBuffTimer();
        timer.Refresh(10f);
        timer.Clear();
        Assert.IsFalse(timer.IsActive);
    }

    [Test]
    public void Limiter_AllowsOnlyOnePerWave() {
        var limiter = new MagnetDropLimiter();
        Assert.IsTrue(limiter.TryClaim(10));
        Assert.IsFalse(limiter.TryClaim(10));
    }

    [Test]
    public void Limiter_DifferentWavesAreIndependent() {
        var limiter = new MagnetDropLimiter();
        Assert.IsTrue(limiter.TryClaim(10));
        Assert.IsTrue(limiter.TryClaim(20));
        Assert.IsFalse(limiter.TryClaim(10));
    }

    [Test]
    public void PullRules_HealthPackOnlyWhenHurt() {
        Assert.IsTrue(MagnetPullRules.ShouldPullHealthPack(60f, 100f));
        Assert.IsFalse(MagnetPullRules.ShouldPullHealthPack(100f, 100f));
        Assert.IsFalse(MagnetPullRules.ShouldPullHealthPack(110f, 110f));
    }

    [Test]
    public void PullRules_AmmoPackOnlyWhenReserveNotFull() {
        Assert.IsTrue(MagnetPullRules.ShouldPullAmmoPack(100, 240));
        Assert.IsFalse(MagnetPullRules.ShouldPullAmmoPack(240, 240));
        Assert.IsFalse(MagnetPullRules.ShouldPullAmmoPack(-1, -1)); // 권총 등 무제한 예비탄
    }
}

