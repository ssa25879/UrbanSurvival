using System.Collections;
using System.Collections.Generic;
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

    private LineRenderer bulletLineRenderer; // 탄알 궤적을 그리기 위한 렌더러(첫 번째 펠릿용, 기존 컴포넌트)
    private readonly List<LineRenderer> bulletLineRenderers = new List<LineRenderer>(); // 펠릿별 궤적 렌더러 풀(산탄총처럼 한 번에 여러 발 나가는 무기가 각각 따로 그리도록)

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
        // 펠릿별 궤적 렌더러가 켜진 채로 남지 않도록 전부 끔(무기 교체 도중 이펙트가 남는 것 방지)
        for (int i = 0; i < bulletLineRenderers.Count; i++)
        {
            bulletLineRenderers[i].enabled = false;
        }

        gunData = data;
        magAmmo = magAmmoValue;
        ammoRemain = reserveAmmoValue;
        state = magAmmo > 0 ? State.Ready : State.Empty;
        lastFireTime = 0f;
    }

    // 발사 시도. 실제로 총알이 나갔으면 true
    // characterForward: 발사 방향 기준(총구 자체의 forward가 아니라 캐릭터가 조준 중인 정면 방향을 사용)
    public bool Fire(Vector3 characterForward) {
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
        if (IsMuzzleObstructed(characterForward))
        {
            return false;
        }

        // 마지막 발사 시점 갱신
        lastFireTime = Time.time;
        // 발사 처리 실행
        Shot(characterForward);
        return true;
    }

    // 총구가 벽(Environment 레이어)에 파묻혀 있는지 검사
    // 총구 지점 자체를 구체 검사하면 서 있는 자세에서 총구 높이가 낮을 때 바닥(Environment 레이어)에
    // 항상 걸리는 오검출이 발생하므로, 총몸 쪽 지점에서 총구까지의 짧은 구간을 스윕해 그 사이를
    // 가로막는 벽이 있는지만 검사한다(바닥은 이 구간의 아래쪽에 있어 걸리지 않음)
    private bool IsMuzzleObstructed(Vector3 characterForward) {
        if (muzzleObstructionMask == 0)
        {
            return false;
        }

        Vector3 origin = fireTransform.position - characterForward * muzzleCheckBackOffset;
        RaycastHit hit;
        return Physics.SphereCast(origin, muzzleCheckRadius, characterForward, out hit, muzzleCheckBackOffset, muzzleObstructionMask, QueryTriggerInteraction.Ignore);
    }

    // 실제 발사 처리
    // 발사 방향은 손에 든 총 모델(팔 IK 영향으로 방향이 부정확) 대신 캐릭터의 조준 정면(characterForward)을 사용,
    // 발사 시작 위치(fireTransform.position)는 그대로 총구 지점을 사용한다
    private void Shot(Vector3 characterForward) {
        int pelletCount = Mathf.Max(1, gunData.pelletsPerShot);
        Vector3[] hitPositions = new Vector3[pelletCount]; // 펠릿마다 궤적을 따로 그리기 위해 전부 기록(산탄총 여러 발 발사 연출)

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 shotDirection = ApplySpread(characterForward, gunData.spreadHalfAngle);

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
                    // 누적 점수 기반 영구 피해량 배율 적용(GameManager 미존재 시 1배)
                    float damageMultiplier = GameManager.instance != null ? GameManager.instance.damageMultiplier : 1f;

                    // 상대방 OnDamage 함수 실행(산탄총은 펠릿마다 개별 판정)
                    target.OnDamage(gunData.damage * damageMultiplier, hit.point, hit.normal);
                }

                hitPositions[i] = hit.point;
            }
            else
            {
                // 레이가 충돌 안함 - 최대 사정거리까지 날아갔을 때 위치를 충돌위치로
                hitPositions[i] = fireTransform.position + shotDirection * gunData.range;
            }
        }

        // 발사 이펙트 코루틴으로 재생(펠릿마다 궤적을 하나씩 그려 여러 발이 나가는 느낌을 줌)
        StartCoroutine(ShotEffect(hitPositions, characterForward));

        // 남은 탄약 -1(펠릿 수와 무관하게 1회 발사당 탄창 1발 소모)
        magAmmo--;
        if (magAmmo <= 0)
        {
            // 탄약 전체 소모시
            state = State.Empty;
        }
    }

    // 발사 방향에 무기별 산포를 적용(좌우로만 퍼짐)
    // 상하(pitch)까지 산포시키면 탑뷰 특성상 총알이 바닥에 박히거나 적 키를 넘어가 버려
    // 명중이 거의 안 되는 문제가 있었음(사용자 제보) - 좌우(yaw) 회전만 적용
    private Vector3 ApplySpread(Vector3 forward, float spreadHalfAngleDeg) {
        if (spreadHalfAngleDeg <= 0f)
        {
            return forward;
        }

        float yaw = Random.Range(-spreadHalfAngleDeg, spreadHalfAngleDeg);
        return Quaternion.Euler(0f, yaw, 0f) * forward;
    }

    // 발사 이펙트와 소리를 재생하고 펠릿마다 탄알 궤적을 그림
    // characterForward: 총구화염/탄피배출 이펙트도 총 모델 자체 방향이 아니라 캐릭터 정면을 바라보도록 재생 전 정렬
    private IEnumerator ShotEffect(Vector3[] hitPositions, Vector3 characterForward) {
        // 이펙트를 캐릭터 조준 정면 방향으로 정렬 후 재생
        Quaternion aimRotation = Quaternion.LookRotation(characterForward);
        muzzleFlashEffect.transform.rotation = aimRotation;
        shellEjectEffect.transform.rotation = aimRotation;

        // 총구 화염 재생
        muzzleFlashEffect.Play();
        // 탄피 배출 재생
        shellEjectEffect.Play();

        // 총 발사음 재생
        gunAudioPlayer.PlayOneShot(gunData.shotClip);

        // 펠릿 수만큼 궤적 렌더러를 켬(산탄총은 여러 개가 동시에 켜져 부채꼴로 퍼지는 게 보임)
        for (int i = 0; i < hitPositions.Length; i++)
        {
            LineRenderer lineRenderer = GetLineRenderer(i);
            lineRenderer.SetPosition(0, fireTransform.position);
            lineRenderer.SetPosition(1, hitPositions[i]);
            lineRenderer.enabled = true;
        }

        // 0.03초 동안 잠시 처리를 대기
        yield return new WaitForSeconds(0.03f);

        // 이번에 사용한 궤적 렌더러만 비활성화(다른 무기의 궤적과 겹치지 않게)
        for (int i = 0; i < hitPositions.Length; i++)
        {
            bulletLineRenderers[i].enabled = false;
        }
    }

    // 인덱스에 해당하는 궤적 렌더러를 반환, 없으면 새로 만들어 풀에 추가
    // 인덱스 0은 기존 bulletLineRenderer 컴포넌트를 그대로 사용, 그 이상(산탄총 등)만 동적으로 생성
    private LineRenderer GetLineRenderer(int index) {
        while (bulletLineRenderers.Count <= index)
        {
            LineRenderer newRenderer;
            if (bulletLineRenderers.Count == 0)
            {
                newRenderer = bulletLineRenderer;
            }
            else
            {
                GameObject trailObject = new GameObject("BulletTrail " + bulletLineRenderers.Count);
                trailObject.transform.SetParent(transform, false);
                newRenderer = trailObject.AddComponent<LineRenderer>();
                CopyLineRendererSettings(bulletLineRenderer, newRenderer);
            }

            newRenderer.positionCount = 2;
            newRenderer.enabled = false;
            bulletLineRenderers.Add(newRenderer);
        }

        return bulletLineRenderers[index];
    }

    // 새로 만든 궤적 렌더러가 기존(총구 기준으로 세팅된) 렌더러와 같은 두께·색·머티리얼을 쓰도록 설정 복사
    private static void CopyLineRendererSettings(LineRenderer source, LineRenderer target) {
        target.sharedMaterial = source.sharedMaterial;
        target.startColor = source.startColor;
        target.endColor = source.endColor;
        target.startWidth = source.startWidth;
        target.endWidth = source.endWidth;
        target.widthCurve = source.widthCurve;
        target.numCapVertices = source.numCapVertices;
        target.numCornerVertices = source.numCornerVertices;
        target.textureMode = source.textureMode;
        target.alignment = source.alignment;
        target.useWorldSpace = source.useWorldSpace;
        target.sortingLayerID = source.sortingLayerID;
        target.sortingOrder = source.sortingOrder;
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
