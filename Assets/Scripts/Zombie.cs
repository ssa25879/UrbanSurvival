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
    public AudioClip hitSound; // 피격 시 재생할 소리

    private Animator zombieAnimator; // 애니메이터 컴포넌트
    private AudioSource zombieAudioPlayer; // 오디오 소스 컴포넌트
    private Renderer zombieRenderer; // 렌더러 컴포넌트

    public float damage = 20f; // 공격력
    public float timeBetAttack = 0.5f; // 공격 간격
    private float lastAttackTime; // 마지막 공격 시점

    public ZombieData zombieData { get; private set; } // 이 개체의 셋업 데이터(미니맵 등 UI에서 강화 개체 판별용)

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
        
        // 자식 오브젝트에서 렌더러 컴포넌트 가져오기
        zombieRenderer = GetComponentInChildren<Renderer>();
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

        // 렌더러에 적용된 마테리얼 색 변경 -> 외형 변경
        zombieRenderer.material.color = zombieData.skinColor;
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
                // navMeshAgent의 경로를 갱신하고, 이동
                navMeshAgent.isStopped = false;
                // targetEntity의 위치를 받아 이동경로 갱신
                navMeshAgent.SetDestination(targetEntity.transform.position);
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
            zombieAudioPlayer.PlayOneShot(hitSound);
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
        zombieAudioPlayer.PlayOneShot(deathSound);
        
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
            }
        }
    }
}