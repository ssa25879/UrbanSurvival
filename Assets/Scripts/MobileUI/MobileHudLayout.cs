using UnityEngine;

// 모바일에서 기존 HUD의 총기·탄약 패널(WeaponHUD)을 상단 중앙으로 옮긴다(2026-10-06 사용자 요청).
// 원래 자리(오른쪽 아래)는 발사·재장전 버튼이 쓴다. 씬 파일은 수정하지 않고 런타임에 위치만 바꾼다
// 상단 중앙에는 보스 체력바(Boss Health Panel, 520x76)도 있어서 보스가 살아 있는 동안에는 그 아래로 내린다
public class MobileHudLayout : MonoBehaviour
{
    private const float NormalY = -20f;  // HUD 캔버스 단위. 보스가 없을 때(Boss Health Panel과 같은 위쪽 여백)
    private const float BossAliveY = -104f; // 보스 체력바(위 여백 20 + 높이 76) 아래 8만큼 띄운다

    private RectTransform ammoRect;

    private void Start()
    {
        WeaponHUD hud = FindFirstObjectByType<WeaponHUD>(FindObjectsInactive.Include);
        if (hud == null)
        {
            enabled = false;
            return;
        }

        ammoRect = (RectTransform)hud.transform;
        ammoRect.anchorMin = ammoRect.anchorMax = new Vector2(0.5f, 1f);
        ammoRect.pivot = new Vector2(0.5f, 1f);
        ammoRect.anchoredPosition = new Vector2(0f, NormalY);
    }

    private void LateUpdate()
    {
        if (ammoRect == null)
        {
            return;
        }

        float y = Zombie.bosses.Count > 0 ? BossAliveY : NormalY;
        if (!Mathf.Approximately(ammoRect.anchoredPosition.y, y))
        {
            ammoRect.anchoredPosition = new Vector2(0f, y);
        }
    }
}
