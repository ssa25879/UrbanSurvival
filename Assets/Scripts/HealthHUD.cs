using UnityEngine;
using UnityEngine.UI;

// 좌측 상단 HUD: 현재/최대 체력 수치와 체력 바(Fill Image), 저체력 경고 색상을 표시
public class HealthHUD : MonoBehaviour {
    public Image healthFillImage; // Image.Type = Filled(Horizontal)로 설정된 체력 바
    public Text healthText; // "현재/최대" 수치 텍스트

    public Sprite normalFillSprite; // 평상시(초록) 체력 바 스프라이트
    public Sprite lowHealthFillSprite; // 저체력 경고(빨강) 체력 바 스프라이트
    private readonly float lowHealthRatio = 0.25f; // 최대 체력의 25% 이하일 때 경고색(HUD 준비 문서 9장 초기 제안값)

    private PlayerHealth playerHealth;

    private void Start() {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void Update() {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth == null)
            {
                return;
            }
        }

        float current = playerHealth.health;
        float max = playerHealth.startingHealth;
        float ratio = max > 0f ? current / max : 0f;

        healthFillImage.fillAmount = ratio;
        healthFillImage.sprite = ratio <= lowHealthRatio ? lowHealthFillSprite : normalFillSprite;
        healthText.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);
    }
}
