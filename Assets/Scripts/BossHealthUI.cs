using UnityEngine;
using UnityEngine.UI;

// 화면 상단 중앙의 보스 체력 패널: 살아있는 보스가 있으면 이름·체력바·수치를 표시하고, 없으면 숨긴다
// 보스가 여러 마리면 먼저 등장한 보스를 표시한다
public class BossHealthUI : MonoBehaviour {
    public GameObject panel; // 보스가 있을 때만 켜지는 패널 루트
    public Text nameText; // 보스 이름
    public Image fillImage; // Filled(Horizontal) 체력바
    public Text healthText; // "현재 / 최대" 수치

    private void Update() {
        Zombie boss = FindDisplayedBoss();

        if (boss == null)
        {
            if (panel.activeSelf)
            {
                panel.SetActive(false);
            }
            return;
        }

        if (!panel.activeSelf)
        {
            panel.SetActive(true);
        }

        float ratio = boss.startingHealth > 0f ? Mathf.Clamp01(boss.health / boss.startingHealth) : 0f;
        fillImage.fillAmount = ratio;
        nameText.text = boss.zombieData.displayName;
        healthText.text = Mathf.CeilToInt(boss.health) + " / " + Mathf.RoundToInt(boss.startingHealth);
    }

    // 살아있는 보스 중 가장 먼저 등장한 보스(목록에서 파괴·사망한 보스는 정리)
    private static Zombie FindDisplayedBoss() {
        for (int i = Zombie.bosses.Count - 1; i >= 0; i--)
        {
            if (Zombie.bosses[i] == null || Zombie.bosses[i].dead)
            {
                Zombie.bosses.RemoveAt(i);
            }
        }

        return Zombie.bosses.Count > 0 ? Zombie.bosses[0] : null;
    }
}
