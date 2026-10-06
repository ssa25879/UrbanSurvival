using NUnit.Framework;

public class WeaponDisplayNameTests {
    [TestCase("AK", "AR")]
    [TestCase("ak", "AR")]
    [TestCase("Ak", "AR")]
    public void Ak_IsDisplayedAsAr(string objectName, string expected) {
        Assert.AreEqual(expected, WeaponDisplayName.Get(objectName));
    }

    [TestCase("Pistol")]
    [TestCase("SMG")]
    [TestCase("Shotgun")]
    [TestCase("AKM")]
    public void OtherNames_AreUnchanged(string objectName) {
        Assert.AreEqual(objectName, WeaponDisplayName.Get(objectName));
    }

    [Test]
    public void Null_ReturnsEmpty() {
        Assert.AreEqual(string.Empty, WeaponDisplayName.Get(null));
    }

    [TestCase(0, "PISTOL")]
    [TestCase(1, "AR")]
    [TestCase(2, "SMG")]
    [TestCase(3, "SG")]
    public void SlotShortLabels_AreAbbreviated(int slot, string expected) {
        Assert.AreEqual(expected, WeaponDisplayName.SlotShortLabel(slot));
    }

    [TestCase(-1)]
    [TestCase(4)]
    public void SlotShortLabel_OutOfRange_IsEmpty(int slot) {
        Assert.AreEqual(string.Empty, WeaponDisplayName.SlotShortLabel(slot));
    }
}
