using UnityEngine;

// 처치 드랍으로 획득하는 무기 습득 아이템(소총/SMG/산탄총 전용, 권총은 항상 보유하므로 대상 아님)
public class WeaponPickup : MonoBehaviour, IItem {
    public PlayerShooter.WeaponSlot slot; // 이 아이템이 해금할 무기 슬롯

    public void Use(GameObject target) {
        // 전달 받은 게임 오브젝트로부터 PlayerShooter 컴포넌트를 가져오기 시도
        PlayerShooter playerShooter = target.GetComponent<PlayerShooter>();

        if (playerShooter != null)
        {
            // 해당 슬롯을 해금(이미 보유 중이면 상태 유지, 처음 획득이면 기본 탄약으로 초기화)
            playerShooter.UnlockWeapon(slot);
        }

        // 사용되었으므로, 자신을 파괴
        Destroy(gameObject);
    }
}
