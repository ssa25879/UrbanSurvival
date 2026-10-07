using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class AutoAimTargetingTests {
    private const float Tolerance = 0.0001f;
    private static readonly Vector3 Origin = Vector3.zero;

    private static List<AimCandidate> List(params AimCandidate[] items) {
        return new List<AimCandidate>(items);
    }

    [Test]
    public void PicksNearestAliveEnemyInRange() {
        var enemies = List(
            new AimCandidate(new Vector3(10f, 0f, 0f), true),
            new AimCandidate(new Vector3(0f, 0f, 4f), true));

        AimResult result = AutoAimTargeting.Select(Origin, enemies, 15f, Vector3.zero, Vector3.forward);

        Assert.IsTrue(result.valid);
        Assert.IsTrue(result.hasTarget);
        Assert.AreEqual(0f, result.direction.x, Tolerance);
        Assert.AreEqual(1f, result.direction.z, Tolerance);
    }

    [Test]
    public void IgnoresEnemiesOutsideRange() {
        var enemies = List(new AimCandidate(new Vector3(20f, 0f, 0f), true));

        AimResult result = AutoAimTargeting.Select(Origin, enemies, 15f, new Vector3(0f, 0f, 1f), Vector3.right);

        Assert.IsFalse(result.hasTarget);
        Assert.AreEqual(1f, result.direction.z, Tolerance, "대상이 없으면 이동 방향을 쓴다");
    }

    [Test]
    public void EnemyExactlyAtRange_IsATarget() {
        var enemies = List(new AimCandidate(new Vector3(15f, 0f, 0f), true));
        AimResult result = AutoAimTargeting.Select(Origin, enemies, 15f, Vector3.zero, Vector3.forward);
        Assert.IsTrue(result.hasTarget);
    }

    [Test]
    public void ExcludesDeadEnemies() {
        var enemies = List(
            new AimCandidate(new Vector3(0f, 0f, 2f), false),
            new AimCandidate(new Vector3(8f, 0f, 0f), true));

        AimResult result = AutoAimTargeting.Select(Origin, enemies, 15f, Vector3.zero, Vector3.forward);

        Assert.IsTrue(result.hasTarget);
        Assert.AreEqual(1f, result.direction.x, Tolerance);
    }

    [Test]
    public void NoTarget_UsesMoveDirection() {
        AimResult result = AutoAimTargeting.Select(Origin, List(), 15f, new Vector3(-3f, 0f, 0f), Vector3.forward);
        Assert.IsTrue(result.valid);
        Assert.IsFalse(result.hasTarget);
        Assert.AreEqual(-1f, result.direction.x, Tolerance, "이동 방향은 정규화된다");
    }

    [Test]
    public void NoTargetAndNoMove_KeepsLastDirection() {
        AimResult result = AutoAimTargeting.Select(Origin, List(), 15f, Vector3.zero, Vector3.right);
        Assert.IsTrue(result.valid);
        Assert.AreEqual(1f, result.direction.x, Tolerance);
    }

    [Test]
    public void NothingAvailable_IsInvalid() {
        AimResult result = AutoAimTargeting.Select(Origin, List(), 15f, Vector3.zero, Vector3.zero);
        Assert.IsFalse(result.valid);
    }

    [Test]
    public void EnemyAtSamePosition_IsSkippedWithoutNaN() {
        var enemies = List(
            new AimCandidate(Origin, true),
            new AimCandidate(new Vector3(0f, 0f, 6f), true));

        AimResult result = AutoAimTargeting.Select(Origin, enemies, 15f, Vector3.zero, Vector3.right);

        Assert.IsTrue(result.hasTarget);
        Assert.IsFalse(float.IsNaN(result.direction.x));
        Assert.AreEqual(1f, result.direction.z, Tolerance, "겹친 적은 방향을 만들 수 없어 건너뛴다");
    }

    [Test]
    public void ZeroOrNegativeRange_HasNoTarget() {
        var enemies = List(new AimCandidate(new Vector3(0f, 0f, 1f), true));
        AimResult result = AutoAimTargeting.Select(Origin, enemies, 0f, Vector3.zero, Vector3.right);
        Assert.IsFalse(result.hasTarget);
    }

    [Test]
    public void DistanceIgnoresHeight() {
        var enemies = List(new AimCandidate(new Vector3(0f, 5f, 10f), true));
        AimResult result = AutoAimTargeting.Select(Origin, enemies, 10.5f, Vector3.zero, Vector3.right);
        Assert.IsTrue(result.hasTarget, "수평 거리 10 이므로 사거리 안");
        Assert.AreEqual(0f, result.direction.y, Tolerance);
    }

    [Test]
    public void FiveHundredCandidates_PicksNearestQuickly() {
        var enemies = new List<AimCandidate>();
        for (int i = 0; i < 500; i++)
        {
            enemies.Add(new AimCandidate(new Vector3(10f + i * 0.01f, 0f, 0f), true));
        }
        enemies.Add(new AimCandidate(new Vector3(0f, 0f, 3f), true));

        AimResult result = AutoAimTargeting.Select(Origin, enemies, 30f, Vector3.zero, Vector3.right);

        Assert.AreEqual(1f, result.direction.z, Tolerance);
    }
}
