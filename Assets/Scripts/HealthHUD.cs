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
    private int shownCurrent = int.MinValue; // 마지막으로 표시한 현재/최대 체력(올림 정수, 같으면 갱신 생략, 2026-10-20)
    private int shownMax = int.MinValue;
    private float shownRatio = -1f;
    private bool shownLow;
    private bool hasShownSprite;

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

        if (!Mathf.Approximately(ratio, shownRatio))
        {
            shownRatio = ratio;
            healthFillImage.fillAmount = ratio;
        }

        bool low = ratio <= lowHealthRatio;
        if (!hasShownSprite || low != shownLow)
        {
            hasShownSprite = true;
            shownLow = low;
            healthFillImage.sprite = low ? lowHealthFillSprite : normalFillSprite;
        }

        int currentInt = Mathf.CeilToInt(current);
        int maxInt = Mathf.CeilToInt(max);
        if (currentInt != shownCurrent || maxInt != shownMax)
        {
            shownCurrent = currentInt;
            shownMax = maxInt;
            healthText.text = currentInt + " / " + maxInt;
        }
    }
}
