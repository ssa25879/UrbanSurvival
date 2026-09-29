using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// AGENTS.md에 확정된 캐릭터 배율과 무기 4종 수치가 데이터 애셋에서 바뀌지 않았는지 확인한다.
// 게임 코드는 Assembly-CSharp에 있어 이 어셈블리에서 타입을 직접 참조할 수 없으므로 SerializedObject로 필드를 읽는다.
public class SpecDataTests {
    private const float Tolerance = 0.0001f;

    private static SerializedObject Load(string path) {
        ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
        Assert.IsNotNull(asset, "데이터 애셋을 찾을 수 없음: " + path);
        return new SerializedObject(asset);
    }

    private static float Float(SerializedObject data, string field) {
        SerializedProperty property = data.FindProperty(field);
        Assert.IsNotNull(property, "필드를 찾을 수 없음: " + field);
        return property.floatValue;
    }

    private static int Int(SerializedObject data, string field) {
        SerializedProperty property = data.FindProperty(field);
        Assert.IsNotNull(property, "필드를 찾을 수 없음: " + field);
        return property.intValue;
    }

    // 이동 배율은 2026-09-29에 A 0.8, B 1.0으로 변경(기준 이동속도 5 → A 4, B 5)
    [TestCase("Assets/ScriptableData/Character A Data.asset", 0.8f, 1.1f, 1.1f, 0.95f, 1.0f)]
    [TestCase("Assets/ScriptableData/Character B Data.asset", 1.0f, 0.9f, 0.9f, 1.05f, 1.0f)]
    public void CharacterMultipliers_MatchSpec(string path, float move, float attackSpeed, float maxHealth, float reloadSpeed, float damage) {
        SerializedObject data = Load(path);

        Assert.AreEqual(move, Float(data, "moveSpeedMultiplier"), Tolerance, "이동속도 배율");
        Assert.AreEqual(attackSpeed, Float(data, "attackSpeedMultiplier"), Tolerance, "공격속도 배율");
        Assert.AreEqual(maxHealth, Float(data, "maxHealthMultiplier"), Tolerance, "최대 체력 배율");
        Assert.AreEqual(reloadSpeed, Float(data, "reloadSpeedMultiplier"), Tolerance, "재장전속도 배율");
        Assert.AreEqual(damage, Float(data, "damageMultiplier"), Tolerance, "공격력 배율");
    }

    // 순서: 피해, 발사 간격, 탄창, 재장전, 사거리, 산포 반각, 펠릿 수, 초기 예비탄, 예비탄 상한(-1=무제한), 입력 모드(0=단발·1회, 1=연사)
    [TestCase("Assets/ScriptableData/Pistol Data.asset", 10f, 0.2f, 10, 2.0f, 15f, 3f, 1, -1, -1, 0)]
    [TestCase("Assets/ScriptableData/Gun Data.asset", 20f, 0.15f, 30, 2.3f, 30f, 3f, 1, 150, 240, 1)]
    [TestCase("Assets/ScriptableData/SMG Data.asset", 12f, 0.09f, 40, 2.1f, 20f, 5f, 1, 200, 320, 1)]
    [TestCase("Assets/ScriptableData/Shotgun Data.asset", 10f, 0.75f, 8, 2.7f, 12f, 10f, 6, 40, 64, 0)]
    public void WeaponStats_MatchSpec(string path, float damage, float timeBetFire, int magCapacity, float reloadTime,
        float range, float spreadHalfAngle, int pellets, int startAmmo, int reserveCap, int fireMode) {
        SerializedObject data = Load(path);

        Assert.AreEqual(damage, Float(data, "damage"), Tolerance, "피해");
        Assert.AreEqual(timeBetFire, Float(data, "timeBetFire"), Tolerance, "발사 간격");
        Assert.AreEqual(magCapacity, Int(data, "magCapacity"), "탄창");
        Assert.AreEqual(reloadTime, Float(data, "reloadTime"), Tolerance, "재장전 시간");
        Assert.AreEqual(range, Float(data, "range"), Tolerance, "사거리");
        Assert.AreEqual(spreadHalfAngle, Float(data, "spreadHalfAngle"), Tolerance, "산포 반각");
        Assert.AreEqual(pellets, Int(data, "pelletsPerShot"), "펠릿 수");
        Assert.AreEqual(startAmmo, Int(data, "startAmmoRemain"), "초기 예비탄");
        Assert.AreEqual(reserveCap, Int(data, "reserveAmmoCap"), "예비탄 상한");
        Assert.AreEqual(fireMode, data.FindProperty("fireMode").enumValueIndex, "입력 모드");
    }

    // 보스 체력 기준값(2026-09-29 확정): 10분·20분 보스 300, 30분 이후 보스 600. 이동속도·공격력·크기·점수는 초기 제안값이라 검사하지 않는다
    [TestCase("Assets/ScriptableData/Zombie Boss.asset", 300f)]
    [TestCase("Assets/ScriptableData/Zombie Final Boss.asset", 600f)]
    public void BossHealth_MatchesSpec(string path, float health) {
        SerializedObject data = Load(path);

        Assert.AreEqual(health, Float(data, "health"), Tolerance, "보스 체력");
        Assert.IsTrue(data.FindProperty("isBoss").boolValue, "isBoss");
        Assert.IsTrue(data.FindProperty("isElite").boolValue, "보스는 미니맵에서 강화 개체로 표시");
    }

    // 처치 점수(2026-09-29 확정): 일반 100, 강화(Heavy) 250, 보스 500 / 1000
    [TestCase("Assets/ScriptableData/Zombie Default.asset", 100)]
    [TestCase("Assets/ScriptableData/Zombie Fast.asset", 100)]
    [TestCase("Assets/ScriptableData/Zombie Heavy.asset", 250)]
    [TestCase("Assets/ScriptableData/Zombie Boss.asset", 500)]
    [TestCase("Assets/ScriptableData/Zombie Final Boss.asset", 1000)]
    public void KillScore_MatchesSpec(string path, int score) {
        Assert.AreEqual(score, Int(Load(path), "score"), "처치 점수");
    }

    // 산탄총은 산탄당 10 x 6개로 근접 전부 명중 시 총 피해 60(기획서 F15)
    [Test]
    public void Shotgun_TotalPelletDamage_Is60() {
        SerializedObject data = Load("Assets/ScriptableData/Shotgun Data.asset");

        Assert.AreEqual(60f, Float(data, "damage") * Int(data, "pelletsPerShot"), Tolerance);
    }
}
