using NUnit.Framework;

public class MobileFireLogicTests {
    private static MobileFireFrame Frame(bool held, bool down, MobileGunState state, bool automatic, bool reload = false) {
        return new MobileFireFrame {
            held = held, down = down, gunState = state, gunIsAutomatic = automatic, reloadRequested = reload
        };
    }

    // 발사 규칙은 조준 모드(오토 에임 FIRE 버튼 / 쌍둥이 스틱)와 무관하게 같다(2026-10-06 사용자 결정).
    // PlayerInput은 오토 에임에서는 FIRE 버튼, 쌍둥이 스틱에서는 조준 스틱 당김을 held/down으로 넘긴다

    [Test]
    public void ReloadButton_PassesThrough() {
        var logic = new MobileFireLogic();
        MobileFireResult r = logic.Evaluate(Frame(false, false, MobileGunState.Ready, true, reload: true));
        Assert.IsTrue(r.reload);
    }

    // ---------- 누른 채 빈 탄창: 자동 재장전 ----------

    [TestCase(true)]
    [TestCase(false)]
    public void HeldWithEmptyMag_RequestsReload(bool automatic) {
        var logic = new MobileFireLogic();
        MobileFireResult r = logic.Evaluate(Frame(true, false, MobileGunState.Empty, automatic));
        Assert.IsTrue(r.reload, "발사 입력을 누른 채 탄창이 비면 자동 재장전한다(연사·단발 모두)");
        Assert.IsFalse(r.fireDown, "빈 탄창에서 합성 fireDown은 내지 않는다(재장전은 reload로 처리)");
    }

    [Test]
    public void NotHeldWithEmptyMag_DoesNotReload() {
        var logic = new MobileFireLogic();
        Assert.IsFalse(logic.Evaluate(Frame(false, false, MobileGunState.Empty, true)).reload);
    }

    // ---------- 재장전 뒤 계속 발사 ----------

    [Test]
    public void HeldThroughReload_SuppressesDuringThenResumesOnce_Automatic() {
        var logic = new MobileFireLogic();

        MobileFireResult during = logic.Evaluate(Frame(true, false, MobileGunState.Reloading, true));
        Assert.IsFalse(during.fire, "재장전 중에는 발사 입력을 거둔다(PlayerShooter의 재장전 후 대기를 풀기 위해)");
        Assert.IsFalse(during.fireDown);

        MobileFireResult resume = logic.Evaluate(Frame(true, false, MobileGunState.Ready, true));
        Assert.IsTrue(resume.fire);
        Assert.IsTrue(resume.fireDown, "끝난 첫 프레임에 fireDown을 한 번 낸다");

        MobileFireResult next = logic.Evaluate(Frame(true, false, MobileGunState.Ready, true));
        Assert.IsTrue(next.fire);
        Assert.IsFalse(next.fireDown, "연사 무기는 이후 fireDown 없이 fire 유지로 발사한다");
    }

    [Test]
    public void HeldThroughReload_ResumesManualWeapon() {
        var logic = new MobileFireLogic();
        logic.Evaluate(Frame(true, false, MobileGunState.Reloading, false));

        MobileFireResult resume = logic.Evaluate(Frame(true, false, MobileGunState.Ready, false));
        Assert.IsTrue(resume.fire);
        Assert.IsTrue(resume.fireDown, "단발 무기도 재장전이 끝나면 바로 다시 쏜다");
    }

    [Test]
    public void ReleasedDuringReload_DoesNotResume() {
        var logic = new MobileFireLogic();
        logic.Evaluate(Frame(true, false, MobileGunState.Reloading, true));

        MobileFireResult afterRelease = logic.Evaluate(Frame(false, false, MobileGunState.Reloading, true));
        Assert.IsFalse(afterRelease.fire);

        MobileFireResult ready = logic.Evaluate(Frame(false, false, MobileGunState.Ready, true));
        Assert.IsFalse(ready.fire);
        Assert.IsFalse(ready.fireDown);
    }

    [Test]
    public void ReleaseThenRepress_AfterReload_IsAFreshPress() {
        var logic = new MobileFireLogic();
        logic.Evaluate(Frame(true, false, MobileGunState.Reloading, true));
        logic.Evaluate(Frame(false, false, MobileGunState.Ready, true));

        MobileFireResult repress = logic.Evaluate(Frame(true, true, MobileGunState.Ready, true));
        Assert.IsTrue(repress.fire);
        Assert.IsTrue(repress.fireDown);
    }

    [Test]
    public void Reset_ClearsPendingResume() {
        var logic = new MobileFireLogic();
        logic.Evaluate(Frame(true, false, MobileGunState.Reloading, true));
        logic.Reset(); // 조준 모드 전환·정지·포커스 변경

        MobileFireResult r = logic.Evaluate(Frame(true, false, MobileGunState.Ready, true));
        Assert.IsFalse(r.fireDown, "리셋 뒤에는 재개용 fireDown을 만들지 않는다");
    }

    // ---------- 단발(Manual) 무기 자동 반복 ----------

    [Test]
    public void ManualWeapon_HeldAndReady_RepeatsFireDown() {
        var logic = new MobileFireLogic();
        MobileFireResult r = logic.Evaluate(Frame(true, false, MobileGunState.Ready, false));
        Assert.IsTrue(r.fireDown);
    }

    [Test]
    public void ManualWeapon_NotHeld_DoesNotFire() {
        var logic = new MobileFireLogic();
        MobileFireResult r = logic.Evaluate(Frame(false, false, MobileGunState.Ready, false));
        Assert.IsFalse(r.fireDown);
        Assert.IsFalse(r.fire);
    }

    [Test]
    public void AutomaticWeapon_Held_DoesNotForceFireDown() {
        var logic = new MobileFireLogic();
        MobileFireResult r = logic.Evaluate(Frame(true, false, MobileGunState.Ready, true));
        Assert.IsTrue(r.fire);
        Assert.IsFalse(r.fireDown);
    }

    [Test]
    public void NoGun_NeverFiresSynthetically() {
        var logic = new MobileFireLogic();
        MobileFireResult r = logic.Evaluate(Frame(true, false, MobileGunState.None, false));
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
