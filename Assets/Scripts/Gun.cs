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

    private LineRenderer bulletLineRenderer; // 탄알 궤적을 그리기 위한 렌더러

    private AudioSource gunAudioPlayer; // 총 소리 재생기

    public GunData gunData; // 총의 현재 데이터

    private float fireDistance = 50f; // 사정거리

    public int ammoRemain = 100; // 남은 전체 탄알
    public int magAmmo; // 현재 탄알집에 남아 있는 탄알

    private float lastFireTime; // 총을 마지막으로 발사한 시점

    private void Awake() {
        // 사용할 컴포넌트의 참조 가져오기
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();
        
        // 사용할 점을 두 개로 변경
        bulletLineRenderer.positionCount = 2;
        
        // 라인 렌더러 비활성화
        bulletLineRenderer.enabled = false;
    }

    private void OnEnable() {
        // 총 상태 초기화
        ammoRemain = gunData.startAmmoRemain;
        // 현재 탄창 채우기
        magAmmo = gunData.magCapacity;
        
        // 총의 상태를 발사 가능 상태로 변경
        state = State.Ready;
        // 총 쏜 시점을 초기화
        lastFireTime = 0;
    }

    // 발사 시도
    public void Fire() {
        // 발사 가능 상태 && 마지막 발사 시점으로부터 gunData.timeBetFire 이상의 시간이 지남
        if (state == State.Ready && Time.time >= lastFireTime + gunData.timeBetFire)
        {
            // 마지막 발사 시점 갱신
            lastFireTime = Time.time;
            // 발사 처리 실행
            Shot();
        }
    }

    // 실제 발사 처리
    private void Shot() {
        // 레이캐스트 저장용 컨테이너
        RaycastHit hit;
        
        // 탄알이 맞은 위치를 저장할 변수
        Vector3 hitPosition = Vector3.zero;
        
        // 레이캐스트
        if (Physics.Raycast(fireTransform.position, fireTransform.forward, out hit, fireDistance))
        {
            // 레이가 충돌 한 경우
            // 충돌한 상대로부터 IDamageable 오브젝트 가져오기
            IDamageable target = hit.collider.GetComponent<IDamageable>();

            if (target != null)
            {
                // 상대방 OnDamage 함수 실행
                target.OnDamage(gunData.damage, hit.point, hit.normal);
            }
            
            // 충돌한 위치 저장
            hitPosition = hit.point;
        }
        else
        {
            // 레이가 충돌 안함
            // 최대 사정거리까지 날아갔을 때 위치를 충돌위치로
            hitPosition = fireTransform.position + fireTransform.forward * fireDistance;
        }
        
        // 발사 이펙트 코루틴으로 재생
        StartCoroutine(ShotEffect(hitPosition));
        
        // 남은 탄약 -1
        magAmmo--;
        if (magAmmo <= 0)
        {
            // 탄약 전체 소모시
            state = State.Empty;
        }
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
        if (state == State.Reloading || ammoRemain <= 0 || magAmmo >= gunData.magCapacity)
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
        
        // 탄창에 채울 탄약이 전체 탄악량보다 많다면, 채울 탄알 수를 남은 전체 탄약량에 맞춰 줄임
        if (ammoRemain < ammoToFill)
        {
            ammoToFill = ammoRemain;
        }
        
        // 장전을 함
        magAmmo += ammoToFill;
        ammoRemain -= ammoToFill;

        // 총의 현재 상태를 발사 준비된 상태로 변경
        state = State.Ready;
    }
}