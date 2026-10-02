using UnityEngine;

// 보스의 원거리 패턴(2026-10-01 추가): 일정 간격마다 바닥에 레드존(위험 지역)을 만들고 경고 뒤 범위 피해를 준다.
// - 지정 공격: 플레이어가 있는 자리에 레드존(보스 데이터의 개수만큼, 둘째부터는 플레이어 주변에 어긋나게)
// - 내려찍기: 일정 횟수마다 보스 자신을 중심으로 넓은 레드존(붙어 있는 플레이어를 떼어 내는 용도)
// 수치(간격·범위·피해 배율·경고 시간)는 모두 ZombieData에서 읽는다. ZombieSpawner.CreateZombie에서 붙인다
public class BossRedZoneAttack : MonoBehaviour {
    private Zombie zombie;
    private ZombieData data;
    private PlayerHealth player;
    private float nextCastTime;
    private int castCount;
    private Animator animator;
    private bool hasAttackTrigger; // 모델 애니메이터에 "Attack" 트리거가 있는지(Arm의 Punch 모션)

    public static BossRedZoneAttack Attach(Zombie zombie) {
        BossRedZoneAttack attack = zombie.gameObject.AddComponent<BossRedZoneAttack>();
        attack.zombie = zombie;
        attack.data = zombie.zombieData;
        attack.nextCastTime = Time.time + attack.data.rangedFirstDelay;
        attack.animator = zombie.GetComponentInChildren<Animator>();
        if (attack.animator != null)
        {
            foreach (AnimatorControllerParameter parameter in attack.animator.parameters)
            {
                if (parameter.name == "Attack" && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    attack.hasAttackTrigger = true;
                }
            }
        }
        return attack;
    }

    private void Update() {
        if (zombie == null || zombie.dead || data == null || data.rangedZonePrefab == null)
        {
            return;
        }

        GameManager manager = GameManager.instance;
        if (manager != null && (manager.isGameover || manager.isPaused))
        {
            return;
        }

        if (Time.time < nextCastTime)
        {
            return;
        }

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerHealth>();
        }
        if (player == null || player.dead)
        {
            return;
        }

        Cast();
        nextCastTime = Time.time + data.rangedInterval;
    }

    // Punch 클립에서 팔이 뻗어 닿는 시점(클립 시작 후 초). 모션을 이만큼 앞당겨 시작해 폭발 순간에 타격이 맞는다
    private const float PunchImpactSeconds = 0.4f;

    // 경고가 끝나는 순간(폭발·피해 판정 시점)에 타격이 오도록 보스가 공격 모션을 미리 시작한다
    private void ScheduleAttackMotion() {
        if (hasAttackTrigger)
        {
            Invoke(nameof(PlayAttackMotion), Mathf.Max(0f, data.rangedWarnSeconds - PunchImpactSeconds));
        }
    }

    private void PlayAttackMotion() {
        if (zombie != null && !zombie.dead && animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    private void Cast() {
        castCount++;
        // 보스의 현재 공격력(시간 비례 배율 반영)에 데이터의 배율을 곱한 값이 레드존 피해
        float damage = zombie.damage * data.rangedDamageMultiplier;
        float radius = data.rangedZoneRadius;

        bool slam = data.rangedSlamEvery > 0 && castCount % data.rangedSlamEvery == 0;
        if (slam)
        {
            // 내려찍기: 보스 자신을 중심으로 더 넓게
            RedZone.Spawn(data.rangedZonePrefab, zombie.transform.position, radius * 1.4f, data.rangedWarnSeconds, damage, 0f, data.rangedPercentMaxHealth);
            ScheduleAttackMotion();
            return;
        }

        Vector3 target = player.transform.position;
        int count = Mathf.Max(1, data.rangedZoneCount);
        for (int i = 0; i < count; i++)
        {
            Vector3 center = target;
            if (i > 0)
            {
                // 둘째부터는 플레이어 주변의 어긋난 자리(한 곳에만 서 있으면 맞지 않도록)
                Vector2 around = Random.insideUnitCircle.normalized * radius * Random.Range(1.4f, 2.2f);
                center += new Vector3(around.x, 0f, around.y);
            }

            // 여러 곳도 한꺼번에 폭발한다(경고 시간이 같아야 한 번의 공격 모션과 맞는다)
            RedZone.Spawn(data.rangedZonePrefab, center, radius, data.rangedWarnSeconds, damage, 0f, data.rangedPercentMaxHealth);
        }

        ScheduleAttackMotion();
    }
}
