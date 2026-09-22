using UnityEngine;

// 현재 장착 중인 무기의 예비 탄약을 충전하는 아이템
public class AmmoPack : MonoBehaviour, IItem {
    public int ammo = 30; // 충전할 총알 수

    public void Use(GameObject target) {
        // 전달 받은 게임 오브젝트로부터 PlayerShooter 컴포넌트를 가져오기 시도
        PlayerShooter playerShooter = target.GetComponent<PlayerShooter>();

        if (playerShooter != null)
        {
            // 현재 장착한 무기의 예비 탄약을 ammo만큼 채움(상한 초과분은 버려짐, 무제한 무기는 무시)
            playerShooter.AddAmmoToCurrentWeapon(ammo);
        }

        // 사용되었으므로, 자신을 파괴
        Destroy(gameObject);
    }
}