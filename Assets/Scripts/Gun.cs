using System.Collections;
using UnityEngine;

// 총을 구현
public class Gun : MonoBehaviour {
    // 총의 상태를 표현하는 데 사용할 타입을 선언
    public enum State {
        Ready, // 발사 준비됨
        Empty, // 탄알집이 빔
        Reloading // 재장전 중
    }

    public State state { get; private set; } // 현재 총의 상태

    public Transform fireTransform; // 탄알이 발사될 위치

    public ParticleSystem muzzleFlashEffect; // 총구 화염 효과
    public ParticleSystem shellEjectEffect; // 탄피 배출 효과

    public LayerMask hitLayers = ~0; // 총알이 명중 판정할 레이어(씬에서 Player 레이어 제외하도록 설정)
    public float muzzleCheckRadius = 0.05f; // 총구 장애물(벽) 검사 두께(SphereCast 반경)
    public float muzzleCheckBackOffset = 0.3f; // 총구 기준 총몸 쪽으로 검사 시작점을 옮기는 거리(바닥 오검출 방지용, 총구 지점 자체가 아니라 총구 앞 구간을 검사)

    private int muzzleObstructionMask; // 총구 장애물 검사 전용 레이어 마스크(Environment만)

    private LineRenderer bulletLineRenderer; // 탄알 궤적을 그리기 위한 렌더러

    private AudioSource gunAudioPlayer; // 총 소리 재생기

    public GunData gunData; // 총의 현재 데이터

    public int ammoRemain; // 남은 전체 탄알(-1이면 무제한)
    public int magAmmo; // 현재 탄알집에 남아 있는 탄알

    private bool ammoInitialized; // 무기 오브젝트가 처음 활성화될 때 한 번만 초기 탄약을 채우기 위한 가드

    private float lastFireTime; // 총을 마지막으로 발사한 시점

    private void Awake() {
        // 사용할 컴포넌트의 참조 가져오기
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();

        // 사용할 점을 두 개로 변경
        bulletLineRenderer.positionCount = 2;

        // 라인 렌더러 비활성화
        bulletLineRenderer.enabled = false;

        // 총구 장애물 검사는 Environment 레이어에만 반응(좀비 근접으로 인한 오검출 방지)
        int environmentLayer = LayerMask.NameToLayer("Environment");
        muzzleObstructionMask = environmentLayer >= 0 ? (1 << environmentLayer) : 0;
    }

    private void OnEnable() {
        // 무기 슬롯 전환으로 재활성화될 때는 기존 탄약 상태를 그대로 유지하고,
        // 최초 1회(획득/장착 시점)에만 gunData 기준으로 탄약을 채운다
        if (!ammoInitialized && gunData != null)
        {
            ConfigureSlot(gunData, gunData.magCapacity, gunData.startAmmoRemain);
            ammoInitialized = true;
        }
    }

    // 무기 슬롯을 교체할 때 총의 데이터와 탄약 상태를 갱신
    public void ConfigureSlot(GunData data, int magAmmoValue, int reserveAmmoValue) {
        // 진행 중이던 재장전 등 코루틴 정리
        StopAllCoroutines();
        if (bulletLineRenderer != null)
        {
            bulletLineRenderer.enabled = false;
        }

        gunData = data;
        magAmmo = magAmmoValue;
        ammoRemain = reserveAmmoValue;
        state = magAmmo > 0 ? State.Ready : State.Empty;
        lastFireTime = 0f;
    }

    // 발사 시도. 실제로 총알이 나갔으면 true
    public bool Fire() {
        // 발사 가능 상태 && 마지막 발사 시점으로부터 gunData.timeBetFire 이상의 시간이 지남
        if (state != State.Ready || gunData == null)
        {
            return false;
        }

        if (Time.time < lastFireTime + gunData.timeBetFire)
        {
            return false;
        }

        // 벽에 총구가 막혀 있으면 발사를 거부하고 탄약을 소비하지 않는다
        if (IsMuzzleObstructed())
        {
            return false;
        }

        // 마지막 발사 시점 갱신
        lastFireTime = Time.time;
        // 발사 처리 실행
        Shot();
        return true;
    }

    // 총구가 벽(Environment 레이어)에 파묻혀 있는지 검사
    // 총구 지점 자체를 구체 검사하면 서 있는 자세에서 총구 높이가 낮을 때 바닥(Environment 레이어)에
    // 항상 걸리는 오검출이 발생하므로, 총몸 쪽 지점에서 총구까지의 짧은 구간을 스윕해 그 사이를
    // 가로막는 벽이 있는지만 검사한다(바닥은 이 구간의 아래쪽에 있어 걸리지 않음)
    private bool IsMuzzleObstructed() {
        if (muzzleObstructionMask == 0)
        {
            return false;
        }

        Vector3 origin = fireTransform.position - fireTransform.forward * muzzleCheckBackOffset;
        RaycastHit hit;
        return Physics.SphereCast(origin, muzzleCheckRadius, fireTransform.forward, out hit, muzzleCheckBackOffset, muzzleObstructionMask, QueryTriggerInteraction.Ignore);
    }

    // 실제 발사 처리
    private void Shot() {
        int pelletCount = Mathf.Max(1, gunData.pelletsPerShot);
        Vector3 lastHitPosition = fireTransform.position + fireTransform.forward * gunData.range;

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 shotDirection = ApplySpread(fireTransform.forward, gunData.spreadHalfAngle);

            // 레이캐스트 저장용 컨테이너
            RaycastHit hit;

            // 레이캐스트: 사거리는 무기별 gunData.range, 레이어는 hitLayers, 트리거 콜라이더는 무시
            if (Physics.Raycast(fireTransform.position, shotDirection, out hit, gunData.range, hitLayers, QueryTriggerInteraction.Ignore))
            {
                // 레이가 충돌 한 경우
                // 충돌한 콜라이더 자신 또는 부모에서 IDamageable 탐색(자식 콜라이더에만 붙어 있는 경우 대응)
                IDamageable target = hit.collider.GetComponentInParent<IDamageable>();

                if (target != null)
                {
                    // 상대방 OnDamage 함수 실행(산탄총은 펠릿마다 개별 판정)
                    target.OnDamage(gunData.damage, hit.point, hit.normal);
                }

                // 충돌한 위치 저장(마지막 펠릿 기준으로 궤적 표시)
                lastHitPosition = hit.point;
            }
            else
            {
                // 레이가 충돌 안함 - 최대 사정거리까지 날아갔을 때 위치를 충돌위치로
                lastHitPosition = fireTransform.position + shotDirection * gunData.range;
            }
        }

        // 발사 이펙트 코루틴으로 재생(마지막 펠릿의 궤적만 표시)
        StartCoroutine(ShotEffect(lastHitPosition));

        // 남은 탄약 -1(펠릿 수와 무관하게 1회 발사당 탄창 1발 소모)
        magAmmo--;
        if (magAmmo <= 0)
        {
            // 탄약 전체 소모시
            state = State.Empty;
        }
    }

    // 발사 방향에 무기별 산포(원뿔 근사)를 적용
    private Vector3 ApplySpread(Vector3 forward, float spreadHalfAngleDeg) {
        if (spreadHalfAngleDeg <= 0f)
        {
            return forward;
        }

        float pitch = Random.Range(-spreadHalfAngleDeg, spreadHalfAngleDeg);
        float yaw = Random.Range(-spreadHalfAngleDeg, spreadHalfAngleDeg);
        return Quaternion.Euler(pitch, yaw, 0f) * forward;
    }

    // 발사 이펙트와 소리를 재생하고 탄알 궤적을 그림
    private IEnumerator ShotEffect(Vector3 hitPosition) {
        // 총구 화염 재생
        muzzleFlashEffect.Play();
        // 탄피 배출 재생
        shellEjectEffect.Play();

        // 총 발사음 재생
        gunAudioPlayer.PlayOneShot(gunData.shotClip);

        // 발사 시작점 지정
        bulletLineRenderer.SetPosition(0, fireTransform.position);
        // 판정 끝점은 입력으로 들어온 충돌 위치
        bulletLineRenderer.SetPosition(1, hitPosition);
        // 라인 렌더러를 활성화하여 탄알 궤적을 그림
        bulletLineRenderer.enabled = true;

        // 0.03초 동안 잠시 처리를 대기
        yield return new WaitForSeconds(0.03f);

        // 라인 렌더러를 비활성화하여 탄알 궤적을 지움
        bulletLineRenderer.enabled = false;
    }

    // 재장전 시도
    public bool Reload() {
        bool magFull = magAmmo >= gunData.magCapacity;
        bool noReserve = ammoRemain == 0; // ammoRemain이 음수(무제한)면 절대 해당 안 됨

        if (state == State.Reloading || noReserve || magFull)
        {
            // 재장전중 / 남은 탄약 없음 / 이미 가득 참 - 장전 불가능
            return false;
        }
        // 재장전 시작
        StartCoroutine(ReloadRoutine());
        return true;
    }

    // 실제 재장전 처리를 진행
    private IEnumerator ReloadRoutine() {
        // 현재 상태를 재장전 중 상태로 전환
        state = State.Reloading;
        // 재장전 소리 재생
        gunAudioPlayer.PlayOneShot(gunData.reloadClip);

        // 재장전 소요 시간 만큼 처리 쉬기
        yield return new WaitForSeconds(gunData.reloadTime);

        // 탄약 회복량 계산
        int ammoToFill = gunData.magCapacity - magAmmo;

        if (ammoRemain >= 0)
        {
            // 예비 탄약이 유한한 무기: 탄창에 채울 탄약이 남은 전체 탄약량보다 많다면 줄임
            if (ammoRemain < ammoToFill)
            {
                ammoToFill = ammoRemain;
            }

            ammoRemain -= ammoToFill;
        }
        // ammoRemain < 0(무제한, 권총)인 경우 예비 탄약을 소모하지 않는다

        // 장전을 함
        magAmmo += ammoToFill;

        // 총의 현재 상태를 발사 준비된 상태로 변경
        state = State.Ready;
    }
}
