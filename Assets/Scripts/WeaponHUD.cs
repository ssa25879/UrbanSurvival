using UnityEngine;
using UnityEngine.UI;

// 우측 하단 HUD: 현재 무기 이름과 재장전 상태를 표시(탄창/예비탄 수치는 기존 UIManager.ammoText가 계속 담당)
public class WeaponHUD : MonoBehaviour {
    public Text weaponNameText;
    public GameObject reloadIndicator; // 재장전 중일 때만 활성화할 표시(텍스트/아이콘)

    [Header("탄약 수치 색 경고(2026-09-27 추가)")]
    public Text ammoText; // UIManager.ammoText와 같은 텍스트(숫자 갱신은 UIManager, 색만 여기서 변경)
    public float lowAmmoRatio = 0.25f; // 탄창 용량 대비 이 비율 이하이면 경고색
    public Color normalAmmoColor = new Color(0.95f, 0.95f, 0.93f, 1f);
    public Color lowAmmoColor = new Color(0.93f, 0.74f, 0.36f, 1f);
    public Color emptyAmmoColor = new Color(0.90f, 0.28f, 0.30f, 1f);

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

        weaponNameText.text = WeaponDisplayName.Get(gun.gameObject.name); // AK는 AR로 표기

        if (reloadIndicator != null)
        {
            reloadIndicator.SetActive(gun.state == Gun.State.Reloading);
        }

        if (ammoText != null && gun.gunData != null)
        {
            if (gun.magAmmo <= 0)
            {
                ammoText.color = emptyAmmoColor;
            }
            else if (gun.magAmmo <= Mathf.CeilToInt(gun.gunData.magCapacity * lowAmmoRatio))
            {
                ammoText.color = lowAmmoColor;
            }
            else
            {
                ammoText.color = normalAmmoColor;
            }
        }
    }
}
