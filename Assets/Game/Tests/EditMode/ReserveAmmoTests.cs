using NUnit.Framework;

public class ReserveAmmoTests {
    [Test]
    public void Add_IncreasesBelowCap() {
        Assert.AreEqual(100, ReserveAmmo.Add(50, 50, 240));
    }

    [Test]
    public void Add_ClampsToCap() {
        Assert.AreEqual(240, ReserveAmmo.Add(200, 150, 240));
    }

    [Test]
    public void Add_AlreadyAtCapStaysAtCap() {
        Assert.AreEqual(64, ReserveAmmo.Add(64, 40, 64));
    }

    [Test]
    public void Add_UnlimitedCapLeavesValueUnchanged() {
        Assert.AreEqual(-1, ReserveAmmo.Add(-1, 50, -1));
    }

    [Test]
    public void Add_NonPositiveAmountLeavesValueUnchanged() {
        Assert.AreEqual(10, ReserveAmmo.Add(10, 0, 240));
        Assert.AreEqual(10, ReserveAmmo.Add(10, -5, 240));
    }

    [Test]
    public void Add_ValueAboveCapIsNotIncreased() {
        // 상한 밖 값에 더하면 상한으로 맞춰진다
        Assert.AreEqual(240, ReserveAmmo.Add(250, 10, 240));
    }
}
