using NUnit.Framework;
using UnityEngine;

public class MobileInputStateTests {
    [SetUp]
    public void SetUp() {
        MobileInputState.ResetAll();
    }

    [Test]
    public void FireDownRequest_IsConsumedOnce() {
        MobileInputState.RequestFireDown();
        Assert.IsTrue(MobileInputState.ConsumeFireDown());
        Assert.IsFalse(MobileInputState.ConsumeFireDown());
    }

    [Test]
    public void ReloadRequest_IsConsumedOnce() {
        MobileInputState.RequestReload();
        Assert.IsTrue(MobileInputState.ConsumeReload());
        Assert.IsFalse(MobileInputState.ConsumeReload());
    }

    [Test]
    public void SwapRequest_ReturnsSlotThenMinusOne() {
        MobileInputState.RequestSwap(2);
        Assert.AreEqual(2, MobileInputState.ConsumeSwap());
        Assert.AreEqual(-1, MobileInputState.ConsumeSwap());
    }

    [TestCase(-1)]
    [TestCase(4)]
    [TestCase(99)]
    public void SwapRequest_OutOfRange_IsIgnored(int slot) {
        MobileInputState.RequestSwap(slot);
        Assert.AreEqual(-1, MobileInputState.ConsumeSwap());
    }

    [Test]
    public void ResetAll_ClearsEverything() {
        MobileInputState.Move = new Vector2(1f, 0f);
        MobileInputState.AimStick = new Vector2(0f, 1f);
        MobileInputState.FireHeld = true;
        MobileInputState.RequestFireDown();
        MobileInputState.RequestReload();
        MobileInputState.RequestSwap(1);

        MobileInputState.ResetAll();

        Assert.AreEqual(Vector2.zero, MobileInputState.Move);
        Assert.AreEqual(Vector2.zero, MobileInputState.AimStick);
        Assert.IsFalse(MobileInputState.FireHeld);
        Assert.IsFalse(MobileInputState.ConsumeFireDown());
        Assert.IsFalse(MobileInputState.ConsumeReload());
        Assert.AreEqual(-1, MobileInputState.ConsumeSwap());
    }

    [Test]
    public void MobilePlatform_InEditor_DefaultsToNotMobile_AndFlagTogglesIt() {
        bool original = MobilePlatform.ForceMobileInEditor;
        try
        {
            MobilePlatform.ForceMobileInEditor = false;
            Assert.IsFalse(MobilePlatform.IsMobile, "에디터 기본값은 PC 입력이어야 한다");
            MobilePlatform.ForceMobileInEditor = true;
            Assert.IsTrue(MobilePlatform.IsMobile);
        }
        finally
        {
            MobilePlatform.ForceMobileInEditor = original;
        }
    }
}
