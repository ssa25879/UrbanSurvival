using System.Collections;
using UnityEngine;
using UnityEngine.AI; // AI, 내비게이션 시스템 관련 코드 가져오기

// 좀비 AI 구현
public class Zombie : LivingEntity
{
    public LayerMask whatIsTarget; // 추적 대상 레이어

    private LivingEntity targetEntity; // 추적 대상
    private NavMeshAgent navMeshAgent; // 경로 계산 AI 에이전트

    public ParticleSystem hitEffect; // 피격 시 재생할 파티클 효과
    public AudioClip deathSound; // 사망 시 재생할 소리
    public AudioClip hitSound; // 피격 시 재생할 소리(hitSounds가 비어 있을 때 사용)
    public AudioClip[] hitSounds; // 피격 시 무작위로 하나를 골라 재생할 소리들(2026-10-07)
    [Range(0f, 1f)] public float hitVolume = 1f; // 피격음 볼륨 배율
    [Range(0f, 1f)] public float deathVolume = 1f; // 사망음 볼륨 배율

    private Animator zombieAnimator; // 애니메이터 컴포넌트
    private AudioSource zombieAudioPlayer; // 오디오 소스 컴포넌트

    public float damage = 20f; // 공격력
    public float timeBetAttack = 0.5f; // 공격 간격
    private float lastAttackTime; // 마지막 공격 시점
    private bool hasMeleeTrigger; // 애니메이터에 근접 공격 트리거(MeleeAttack)가 있는지

    public ZombieData zombieData { get; private set; } // 이 개체의 셋업 데이터(미니맵 등 UI에서 강화 개체 판별용)

    public static readonly System.Collections.Generic.List<Zombie> bosses = new System.Collections.Generic.List<Zombie>(); // 살아있는 보스 목록(보스 체력 UI가 참조)
    public bool isBoss { get { return zombieData != null && zombieData.isBoss; } }

    public float noPathRelocateSeconds = 5f; // 플레이어까지 유효한 경로가 이 시간 동안 없으면 재배치를 요청(기획서 10장 "길 막힘")
    private float noPathElapsed; // 유효한 경로가 없는 상태로 지난 시간
    public event System.Action<Zombie> onPathBlocked; // 재배치 요청 이벤트(스폰 지점 선택은 ZombieSpawner가 담당)

    // 추적할 대상이 존재하는지 알려주는 프로퍼티
    private bool hasTarget {
        get
        {
            // 추적할 대상이 존재하고, 대상이 사망하지 않았다면 true
            if (targetEntity != null && !targetEntity.dead)
            {
                return true;
            }

            // 그렇지 않다면 false
            return false;
        }
    }

    private void Awake() {
        // 초기화
        // 컴포넌트
        navMeshAgent = GetComponent<NavMeshAgent>();
        // 애니메이터는 좀비 비주얼 모델(자식 오브젝트)에 붙어있음
        zombieAnimator = GetComponentInChildren<Animator>();
        zombieAudioPlayer = GetComponent<AudioSource>();

        foreach (AnimatorControllerParameter parameter in zombieAnimator.parameters)
        {
            if (parameter.name == "MeleeAttack" && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                hasMeleeTrigger = true;
            }
        }
    }

    // 좀비 AI의 초기 스펙을 결정하는 셋업 메서드
    public void Setup(ZombieData zombieData) {
        Setup(zombieData, 1f);
    }

    // 시간비례 난이도 배율을 적용하는 셋업 메서드(신규 생성분에만 적용, ZombieData 원본은 변경하지 않음)
    public void Setup(ZombieData zombieData, float statMultiplier) {
        this.zombieData = zombieData;

        // 기본 체력 설정
        startingHealth = zombieData.health * statMultiplier;
        health = startingHealth;

        // 기초 공격력 설정
        damage = zombieData.damage * statMultiplier;

        // NavMeshAgent 이동속도 설정(시간비례 배율 미적용)
        navMeshAgent.speed = zombieData.speed;

        // 몸(SkinnedMeshRenderer)의 마테리얼 색을 바꿔 외형을 구분한다.
        // 예전에는 자식 순서상 처음 찾은 렌더러(핏방울 파티클)에 색을 적용해 몸 색이 바뀌지 않았다(2026-09-29 수정)
        // 재질 원래 색(약간 어두운 회색)에 곱해서, 흰색인 일반 좀비는 지금 외형을 그대로 유지한다
        foreach (SkinnedMeshRenderer bodyRenderer in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            Material bodyMaterial = bodyRenderer.material;
            bodyMaterial.color = bodyMaterial.color * zombieData.skinColor;

            // 곱하기만으로는 초록 몸에 붉은 기운을 더할 수 없어, 발광색을 더해 색을 입힌다
            if (zombieData.glowColor.maxColorComponent > 0.001f)
            {
                bodyMaterial.EnableKeyword("_EMISSION");
                bodyMaterial.SetColor("_EmissionColor", zombieData.glowColor);
            }
        }

        // 보스는 크게 표시하고 체력 UI(머리 위 바) 대상으로 등록
        if (!Mathf.Approximately(zombieData.modelScale, 1f))
        {
            transform.localScale *= zombieData.modelScale;
        }

        if (zombieData.isBoss)
        {
            bosses.Add(this);
            BossHeadBar.Attach(this);
        }
    }

    private void OnDestroy() {
        bosses.Remove(this);
    }

    private void Start() {
        // 게임 오브젝트 활성화와 동시에 AI의 추적 루틴 시작
        StartCoroutine(UpdatePath());
    }

    private void Update() {
        // 추적 대상의 존재 여부에 따라 다른 애니메이션 재생
        zombieAnimator.SetBool("HasTarget", hasTarget);
    }

    // 주기적으로 추적할 대상의 위치를 찾아 경로 갱신
    private IEnumerator UpdatePath() {
        // 살아 있는 동안 무한 루프
        while (!dead)
        {
            if (hasTarget)
            {
                // 추적 대상이 있을 경우
                // 직전 갱신에서 계산된 경로가 완전한지 확인(SetDestination 직후에는 경로가 아직 계산 중이라 갱신 전에 확인)
                CheckPathBlocked();

                // navMeshAgent의 경로를 갱신하고, 이동(NavMesh 밖에 있으면 SetDestination이 오류를 내므로 건너뜀)
                if (navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.isStopped = false;
                    // targetEntity의 위치를 받아 이동경로 갱신
                    navMeshAgent.SetDestination(targetEntity.transform.position);
                }
            }
            else
            {
                // 추적 대상이 없기에 정지
                navMeshAgent.isStopped = true;
                
                // 가상의 구를 그려 범위 내에 겹치는 whatIsTarget 레이어를 가진 콜라이더만 가져오도록 필터링
                Collider[] colliders = Physics.OverlapSphere(transform.position, 500f, whatIsTarget);
                
                // 콜라이더 순회하며 dead상태가 아닌 LivingEntity를 검색
                for (int i = 0; i < colliders.Length; i++)
                {
                    // LivingEntity 가져오기
                    LivingEntity livingEntity = colliders[i].GetComponent<LivingEntity>();
                    
                    // 해당 엔티티가 있으며, 생존상태일때
                    if (livingEntity != null && !livingEntity.dead)
                    {
                        // 추적대상 설정
                        targetEntity = livingEntity;
                        // 설정했으니 루프 탈출
                        break;
                    }
                }
            }
            
            // 0.25초 주기로 처리 반복
            yield return new WaitForSeconds(0.25f);
        }
    }

    // 유효한 경로가 없는 상태가 noPathRelocateSeconds 이상 이어지면 재배치를 요청한다
    private void CheckPathBlocked() {
        bool blocked = !navMeshAgent.isOnNavMesh
            || (!navMeshAgent.pathPending && navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete);

        if (!blocked)
        {
            noPathElapsed = 0f;
            return;
        }

        // 경로 갱신 주기(0.25초)마다 한 번씩 누적
        noPathElapsed += 0.25f;
        if (noPathElapsed >= noPathRelocateSeconds)
        {
            noPathElapsed = 0f;
            if (onPathBlocked != null)
            {
                onPathBlocked(this);
            }
        }
    }

    // 지정한 위치로 옮긴다(체력·점수·생존 집계는 그대로 유지)
    public void Relocate(Vector3 position, Quaternion rotation) {
        if (dead)
        {
            return;
        }

        noPathElapsed = 0f;
        transform.rotation = rotation;
        if (navMeshAgent.enabled)
        {
            navMeshAgent.Warp(position);
        }
        else
        {
            transform.position = position;
        }
    }

    // 데미지를 입었을 때 실행할 처리
    public override void OnDamage(float damage, Vector3 hitPoint, Vector3 hitNormal) {
        // 사망상태가 아니라면 파티클 재생
        if (!dead)
        {
            // 공격 받은 지점, 방향으로 효과 재생
            hitEffect.transform.position = hitPoint;
            hitEffect.transform.rotation = Quaternion.LookRotation(hitNormal);
            hitEffect.Play();
            
            // 효과음
            AudioClip clip = hitSounds != null && hitSounds.Length > 0
                ? hitSounds[Random.Range(0, hitSounds.Length)]
                : hitSound;
            zombieAudioPlayer.PlayOneShot(clip, hitVolume);
        }
        
        // LivingEntity의 OnDamage()를 실행하여 데미지 적용
        base.OnDamage(damage, hitPoint, hitNormal);
    }

    // 사망 처리
    public override void Die() {
        // 이미 사망 처리된 경우 중복 실행 방지
        if (dead)
        {
            return;
        }

        // LivingEntity의 Die()를 실행하여 기본 사망 처리 실행
        base.Die();

        // Collider 비활성화
        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
        
        // 추적 중지, NavMesh 컴포넌트 비활성화
        navMeshAgent.isStopped = true;
        navMeshAgent.enabled = false;
        
        // 사망 애니메이션, 효과음 재생
        zombieAnimator.SetTrigger("Die");
        zombieAudioPlayer.PlayOneShot(deathSound, deathVolume);
        
    }

    private void OnTriggerStay(Collider other) {
        // 공격 내부 쿨타임이 아니고 + 사망상태가 아니라면 공격 실행
        if (!dead && Time.time >= lastAttackTime + timeBetAttack)
        {
            // 상대방 LivingEntity 가져오고, 만약 추적 대상과 같다면 공격 실행
            LivingEntity attackTarget = other.GetComponent<LivingEntity>();
            if (attackTarget != null && attackTarget == targetEntity)
            {
                // 내부쿨 갱신
                lastAttackTime = Time.time;
                
                // 피격 위치, 방향을 근삿값 계산
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                Vector3 hitNormal = transform.position - other.transform.position;
                
                // 공격
                attackTarget.OnDamage(damage, hitPoint, hitNormal);

                // 근접 공격 모션(컨트롤러에 MeleeAttack 트리거가 있을 때만, 2026-10-02 추가)
                if (hasMeleeTrigger)
                {
                    zombieAnimator.SetTrigger("MeleeAttack");
                }
            }
        }
    }
}