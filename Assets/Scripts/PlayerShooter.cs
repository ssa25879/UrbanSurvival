using UnityEngine;
using UnityEngine.Animations.Rigging;

// 주어진 Gun 오브젝트를 쏘거나 재장전
// 무기 4종(권총/소총/SMG/산탄총)을 슬롯으로 관리하며, 슬롯 전환 시 실제 손에 든 모델도 함께 바뀐다
// 알맞은 애니메이션을 재생하고 IK를 사용해 캐릭터 왼손이 총의 손잡이에 위치하도록 조정
public class PlayerShooter : MonoBehaviour {
    // 무기 슬롯 순서(키보드 1~4와 대응)
    public enum WeaponSlot {
        Pistol = 0,
        Rifle = 1,
        SMG = 2,
        Shotgun = 3
    }

    private const int SlotCount = 4;

    // 인덱스 = WeaponSlot 순서. 각 오브젝트는 이미 자신의 GunData를 들고 있으며(씬에서 배정됨),
    // 슬롯 전환은 이 오브젝트들을 활성/비활성 전환하는 방식으로 이루어진다(실제 손 모델 교체)
    public Gun[] guns = new Gun[SlotCount];

    public Gun gun { get; private set; } // 현재 활성화된 총(읽기 전용, UI/외부 조회용)

    private TwoBoneIKConstraint leftHandIK; // 왼손 IK 제약(무기 교체 시 target을 현재 무기의 LeftHandGrip으로 재설정)

    private bool[] unlocked = new bool[SlotCount]; // 슬롯 보유 여부(권총은 항상 true)
    private int currentSlot; // 현재 장착 중인 슬롯 인덱스

    private bool blockFireUntilRelease; // 재장전/스왑 직후 발사 버튼을 새로 눌러야 하는 상태

    private PlayerInput playerInput; // 플레이어의 입력
    private Animator playerAnimator; // 애니메이터 컴포넌트

    private void Start() {
        // 사용할 컴포넌트들을 가져오기
        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<Animator>();
        leftHandIK = GetComponentInChildren<TwoBoneIKConstraint>(true);

        // 권총은 항상 보유
        unlocked[(int)WeaponSlot.Pistol] = true;

        // 씬에 배치된 무기 오브젝트 중 활성화되어 있는 것을 시작 슬롯으로 사용(기본값: 권총)
        currentSlot = (int)WeaponSlot.Pistol;
        for (int i = 0; i < SlotCount; i++)
        {
            if (guns[i] != null && guns[i].gameObject.activeSelf)
            {
                currentSlot = i;
                break;
            }
        }

        ActivateSlot(currentSlot);
    }

    private void OnEnable() {
        // 슈터가 활성화될 때 현재 총도 함께 활성화
        if (gun != null)
        {
            gun.gameObject.SetActive(true);
        }
    }

    private void OnDisable() {
        // 슈터가 비활성화될 때 현재 총도 함께 비활성화
        if (gun != null)
        {
            gun.gameObject.SetActive(false);
        }
    }

    private void Update() {
        HandleWeaponSwapInput();

        // 재장전/스왑 직후에는 발사 버튼을 한 번 떼야 다시 발사할 수 있다
        if (blockFireUntilRelease && !playerInput.fire)
        {
            blockFireUntilRelease = false;
        }

        if (gun.state == Gun.State.Empty)
        {
            // 탄창이 빈 상태: 새 발사 입력(엣지) 또는 R 입력으로만 재장전(자동 재장전 없음)
            if (playerInput.fireDown || playerInput.reload)
            {
                TryReload();
            }
        }
        else if (gun.state == Gun.State.Ready)
        {
            bool wantsFire = !blockFireUntilRelease && GetFireInput();

            if (wantsFire)
            {
                gun.Fire();
            }
            else if (playerInput.reload)
            {
                // 발사와 재장전이 동시에 들어오면 발사가 우선(위 if에서 이미 처리됨)
                TryReload();
            }
        }

        // UI에 탄약 수 갱신
        UpdateUI();
    }

    // 현재 장착한 무기의 발사 모드(연사/단발)에 맞는 입력값 반환
    private bool GetFireInput() {
        if (gun.gunData == null)
        {
            return false;
        }

        return gun.gunData.fireMode == GunData.FireMode.Automatic ? playerInput.fire : playerInput.fireDown;
    }

    private void TryReload() {
        if (gun.Reload())
        {
            // 재장전 입력 감지 후 재장전 성공 시 애니메이션 재생
            playerAnimator.SetTrigger("Reload");
            blockFireUntilRelease = true;
        }
    }

    // 1~4번 키 입력에 따라 무기 슬롯 교체
    private void HandleWeaponSwapInput() {
        // 재장전 중에는 무기를 바꾸지 않는다
        if (gun.state == Gun.State.Reloading)
        {
            return;
        }

        int requestedSlot = -1;
        if (playerInput.swapToSlot1) requestedSlot = (int)WeaponSlot.Pistol;
        else if (playerInput.swapToSlot2) requestedSlot = (int)WeaponSlot.Rifle;
        else if (playerInput.swapToSlot3) requestedSlot = (int)WeaponSlot.SMG;
        else if (playerInput.swapToSlot4) requestedSlot = (int)WeaponSlot.Shotgun;

        if (requestedSlot >= 0)
        {
            EquipSlot(requestedSlot);
        }
    }

    // 지정한 슬롯으로 무기 교체(미보유 슬롯 입력은 무시)
    private void EquipSlot(int slotIndex) {
        if (slotIndex == currentSlot || !unlocked[slotIndex] || guns[slotIndex] == null)
        {
            return;
        }

        currentSlot = slotIndex;
        ActivateSlot(currentSlot);
        blockFireUntilRelease = true;
    }

    // 슬롯에 해당하는 무기 오브젝트만 활성화하고 나머지는 비활성화, 왼손 IK 타겟도 교체
    private void ActivateSlot(int slotIndex) {
        for (int i = 0; i < SlotCount; i++)
        {
            if (guns[i] != null)
            {
                guns[i].gameObject.SetActive(i == slotIndex);
            }
        }

        gun = guns[slotIndex];

        if (leftHandIK != null)
        {
            Transform leftHandGrip = gun.transform.Find("LeftHandGrip");
            if (leftHandGrip != null)
            {
                leftHandIK.data.target = leftHandGrip;
            }
        }
    }

    // 처치 드랍으로 무기를 획득했을 때 호출(WeaponPickup에서 사용) — 슬롯을 해금만 하고 자동 장착하지는 않는다
    public void UnlockWeapon(WeaponSlot slot) {
        unlocked[(int)slot] = true;
    }

    // 처치 드랍(추가 탄약) 또는 AmmoPack 아이템이 현재 장착 무기의 예비탄을 채울 때 사용
    public void AddAmmoToCurrentWeapon(int amount) {
        if (gun.gunData == null || gun.gunData.reserveAmmoCap < 0)
        {
            // 예비탄 무제한 무기는 채울 필요 없음
            return;
        }

        gun.ammoRemain = Mathf.Min(gun.ammoRemain + amount, gun.gunData.reserveAmmoCap);
    }

    // 탄약 UI 갱신
    private void UpdateUI() {
        if (gun != null && UIManager.instance != null)
        {
            // UI 매니저의 탄약 텍스트에 탄창의 탄약과 남은 전체 탄약을 표시
            UIManager.instance.UpdateAmmoText(gun.magAmmo, gun.ammoRemain);
        }
    }
}
