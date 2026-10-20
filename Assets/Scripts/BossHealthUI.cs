using UnityEngine;
using UnityEngine.UI;

// 화면 상단 중앙의 보스 체력 패널: 살아있는 보스가 있으면 이름·체력바·수치를 표시하고, 없으면 숨긴다
// 보스가 여러 마리면 먼저 등장한 보스를 표시한다
public class BossHealthUI : MonoBehaviour {
    public GameObject panel; // 보스가 있을 때만 켜지는 패널 루트
    public Text nameText; // 보스 이름
    public Image fillImage; // Filled(Horizontal) 체력바
    public Text healthText; // "현재 / 최대" 수치

    private Zombie shownBoss; // 마지막으로 표시한 보스와 수치(같으면 문자열을 다시 만들지 않는다, 2026-10-20)
    private int shownHealth = int.MinValue;
    private int shownMax = int.MinValue;

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
        if (boss != shownBoss)
        {
            shownBoss = boss;
            shownHealth = int.MinValue;
            nameText.text = boss.zombieData.displayName;
        }

        int healthInt = Mathf.CeilToInt(boss.health);
        int maxInt = Mathf.RoundToInt(boss.startingHealth);
        if (healthInt != shownHealth || maxInt != shownMax)
        {
            shownHealth = healthInt;
            shownMax = maxInt;
            healthText.text = healthInt + " / " + maxInt;
        }
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
