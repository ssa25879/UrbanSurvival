using UnityEngine;
using UnityEngine.UI;

// 우측 하단 HUD: 현재 무기 이름과 재장전 상태를 표시(탄창/예비탄 수치는 기존 UIManager.ammoText가 계속 담당)
public class WeaponHUD : MonoBehaviour {
    public Text weaponNameText;
    public GameObject reloadIndicator; // 재장전 중일 때만 활성화할 표시(텍스트/아이콘)

    private PlayerShooter playerShooter;

    private void Start() {
        playerShooter = FindFirstObjectByType<PlayerShooter>();
    }

    private void Update() {
        if (playerShooter == null)
        {
            playerShooter = FindFirstObjectByType<PlayerShooter>();
            if (playerShooter == null)
            {
                return;
            }
        }

        Gun gun = playerShooter.gun;
        if (gun == null)
        {
            return;
        }

        weaponNameText.text = gun.gameObject.name;

        if (reloadIndicator != null)
        {
            reloadIndicator.SetActive(gun.state == Gun.State.Reloading);
        }
    }
}
