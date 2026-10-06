using NUnit.Framework;

public class MobileFireLogicTests {
    private static MobileFireFrame Frame(bool twin, bool held, bool down, MobileGunState state, bool automatic, bool reload = false) {
        return new MobileFireFrame {
            twinStick = twin, held = held, down = down, gunState = state, gunIsAutomatic = automatic, reloadRequested = reload
        };
    }

    // ---------- 오토 에임(발사 버튼) ----------

    [Test]
    public void AutoAim_HeldWithEmptyMag_DoesNotAutoReload() {
        var logic = new MobileFireLogic();
        MobileFireResult r = logic.Evaluate(Frame(false, true, false, MobileGunState.Empty, true));
        Assert.IsFalse(r.reload, "오토 에임 모드는 빈 탄창에서 자동 재장전하지 않는다(기존 규칙)");
        Assert.IsTrue(r.fire);
    }

    [Test]
    public void AutoAim_ReloadButton_PassesThrough() {
        var logic = new MobileFireLogic();
        MobileFireResult r = logic.Evaluate(Frame(false, false, false, MobileGunState.Ready, true, reload: true));
        Assert.IsTrue(r.reload);
    }

    [Test]
    public void AutoAim_ReloadingWhileHeld_KeepsFireAndNeverResumes() {
        var logic = new MobileFireLogic();
        MobileFireResult during = logic.Evaluate(Frame(false, true, false, MobileGunState.Reloading, true));
        Assert.IsTrue(during.fire, "오토 에임은 재장전 중에도 입력을 거두지 않는다(PlayerShooter의 한 번 놓아야 발사 규칙 유지)");

        MobileFireResult after = logic.Evaluate(Frame(false, true, false, MobileGunState.Ready, true));
        Assert.IsFalse(after.fireDown, "재장전이 끝나도 합성 fireDown을 만들지 않는다");
    }

    // ---------- 쌍둥이 스틱: 자동 재장전 ----------

    [Test]
    public void TwinStick_HeldWithEmptyMag_RequestsReload() {
        var logic = new MobileFireLogic();
        Assert.IsTrue(logic.Evaluate(Frame(true, true, false, MobileGunState.Empty, true)).reload);
    }

    [Test]
    public void TwinStick_NotHeldWithEmptyMag_DoesNotReload() {
        var logic = new MobileFireLogic();
        Assert.IsFalse(logic.Evaluate(Frame(true, false, false, MobileGunState.Empty, true)).reload);
    }

    // ---------- 쌍둥이 스틱: 재장전 뒤 계속 발사 ----------

    [Test]
    public void TwinStick_HeldThroughReload_SuppressesDuringThenResumesOnce_Automatic() {
        var logic = new MobileFireLogic();

        MobileFireResult during = logic.Evaluate(Frame(true, true, false, MobileGunState.Reloading, true));
        Assert.IsFalse(during.fire, "재장전 중에는 발사 입력을 거둔다(PlayerShooter의 재장전 후 대기를 풀기 위해)");
        Assert.IsFalse(during.fireDown);

        MobileFireResult resume = logic.Evaluate(Frame(true, true, false, MobileGunState.Ready, true));
        Assert.IsTrue(resume.fire);
        Assert.IsTrue(resume.fireDown, "끝난 첫 프레임에 fireDown을 한 번 낸다");

        MobileFireResult next = logic.Evaluate(Frame(true, true, false, MobileGunState.Ready, true));
        Assert.IsTrue(next.fire);
        Assert.IsFalse(next.fireDown, "연사 무기는 이후 fireDown 없이 fire 유지로 발사한다");
    }

    [Test]
    public void TwinStick_ReleasedDuringReload_DoesNotResume() {
        var logic = new MobileFireLogic();
        logic.Evaluate(Frame(true, true, false, MobileGunState.Reloading, true));

        MobileFireResult afterRelease = logic.Evaluate(Frame(true, false, false, MobileGunState.Reloading, true));
        Assert.IsFalse(afterRelease.fire);

        MobileFireResult ready = logic.Evaluate(Frame(true, false, false, MobileGunState.Ready, true));
        Assert.IsFalse(ready.fire);
        Assert.IsFalse(ready.fireDown);
    }

    [Test]
    public void TwinStick_ReleaseThenRepull_AfterReload_IsAFreshPress() {
        var logic = new MobileFireLogic();
        logic.Evaluate(Frame(true, true, false, MobileGunState.Reloading, true));
        logic.Evaluate(Frame(true, false, false, MobileGunState.Ready, true));

        MobileFireResult repull = logic.Evaluate(Frame(true, true, true, MobileGunState.Ready, true));
        Assert.IsTrue(repull.fire);
        Assert.IsTrue(repull.fireDown);
    }

    [Test]
    public void Reset_ClearsPendingResume() {
        var logic = new MobileFireLogic();
        logic.Evaluate(Frame(true, true, false, MobileGunState.Reloading, true));
        logic.Reset(); // 조준 모드 전환·정지·포커스 변경

        MobileFireResult r = logic.Evaluate(Frame(true, true, false, MobileGunState.Ready, true));
        Assert.IsFalse(r.fireDown, "리셋 뒤에는 재개용 fireDown을 만들지 않는다");
    }

    [Test]
    public void ModeSwitchToAutoAim_ClearsPendingResume() {
        var logic = new MobileFireLogic();
        logic.Evaluate(Frame(true, true, false, MobileGunState.Reloading, true));
        logic.Evaluate(Frame(false, true, false, MobileGunState.Ready, true)); // 오토 에임 프레임

        MobileFireResult backToTwin = logic.Evaluate(Frame(true, true, false, MobileGunState.Ready, true));
        Assert.IsFalse(backToTwin.fireDown, "다른 모드를 거치면 남은 재개 상태가 사라진다");
    }

    // ---------- 단발(Manual) 무기 자동 반복 ----------

    [TestCase(false)]
    [TestCase(true)]
    public void ManualWeapon_HeldAndReady_RepeatsFireDown(bool twin) {
        var logic = new MobileFireLogic();
        MobileFireResult r = logic.Evaluate(Frame(twin, true, false, MobileGunState.Ready, false));
        Assert.IsTrue(r.fireDown);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ManualWeapon_NotHeld_DoesNotFire(bool twin) {
        var logic = new MobileFireLogic();
        MobileFireResult r = logic.Evaluate(Frame(twin, false, false, MobileGunState.Ready, false));
        Assert.IsFalse(r.fireDown);
        Assert.IsFalse(r.fire);
    }

    [Test]
    public void ManualWeapon_HeldWithEmptyMag_InAutoAim_DoesNotCreateFireDown() {
        var logic = new MobileFireLogic();
        MobileFireResult r = logic.Evaluate(Frame(false, true, false, MobileGunState.Empty, false));
        Assert.IsFalse(r.fireDown, "빈 탄창에서 fireDown이 나가면 의도치 않은 자동 재장전이 일어난다");
        Assert.IsFalse(r.reload);
    }

    [Test]
    public void AutomaticWeapon_Held_DoesNotForceFireDown() {
        var logic = new MobileFireLogic();
        MobileFireResult r = logic.Evaluate(Frame(false, true, false, MobileGunState.Ready, true));
        Assert.IsTrue(r.fire);
        Assert.IsFalse(r.fireDown);
    }

    [Test]
    public void NoGun_NeverFiresSynthetically() {
        var logic = new MobileFireLogic();
        MobileFireResult r = logic.Evaluate(Frame(true, true, false, MobileGunState.None, false));
        Assert.IsFalse(r.fireDown);
        Assert.IsFalse(r.reload);
    }

    // ---------- 발사 래치(정지·포커스·모드 전환 뒤 오발 방지) ----------

    [Test]
    public void Latch_BlocksFireWhileHeld_UntilReleased_ThenAllowsNewPress() {
        var latch = new FireLatch();
        latch.RequireRelease();

        bool fire = true, fireDown = true;
        latch.Apply(ref fire, ref fireDown);
        Assert.IsFalse(fire);
        Assert.IsFalse(fireDown);

        fire = true; fireDown = false; // 계속 누르는 중
        latch.Apply(ref fire, ref fireDown);
        Assert.IsFalse(fire, "누른 채로 남아 있으면 계속 막는다");

        fire = false; fireDown = false; // 손을 뗌
        latch.Apply(ref fire, ref fireDown);
        Assert.IsFalse(latch.Suppressing);

        fire = true; fireDown = true; // 새로 누름
        latch.Apply(ref fire, ref fireDown);
        Assert.IsTrue(fire);
        Assert.IsTrue(fireDown);
    }

    [Test]
    public void Latch_WhenNotHeld_ClearsOnNextApply() {
        var latch = new FireLatch();
        latch.RequireRelease();
        bool fire = false, fireDown = false;
        latch.Apply(ref fire, ref fireDown);
        Assert.IsFalse(latch.Suppressing);
    }

    [Test]
    public void Latch_NotRequired_PassesInputThrough() {
        var latch = new FireLatch();
        bool fire = true, fireDown = true;
        latch.Apply(ref fire, ref fireDown);
        Assert.IsTrue(fire);
        Assert.IsTrue(fireDown);
    }
}
