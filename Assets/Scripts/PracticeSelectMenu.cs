using UnityEngine;
using UnityEngine.UI;

// 인트로의 연습(PRACTICE) 패널(2026-10-01 추가): VS BOSS / VS FINAL BOSS 탭을 고르고 해당 연습 씬으로 들어간다.
// 탭에는 보스 수치(데이터에서 계산)와 최고 처치 기록을 표시하고, 연습에 쓸 캐릭터(A/B)를 바꿀 수 있다
public class PracticeSelectMenu : MonoBehaviour {
    public IntroMenu introMenu; // 씬 전환 담당
    public CharacterSelectMenu characterSelect; // 캐릭터 목록(A/B) 참조
    public GameObject panel; // 패널 루트(씬에서 비활성 상태로 시작)
    public Image[] tabFrames; // 탭 버튼 배경(0 = VS BOSS, 1 = VS FINAL BOSS)
    public Text[] tabTexts; // 탭 문구
    public Text infoText; // 선택한 탭의 보스 정보
    public Text bestText; // 최고 기록
    public Text characterButtonText; // 캐릭터 전환 버튼 문구
    public ZombieData[] bossDatas; // 탭별 보스 데이터(0 = Boss, 1 = Final Boss)
    public float[] startMinutes = { 10f, 30f }; // 탭별 게임 진행 시간(분) = 연습 씬의 GameManager.startElapsedMinutes와 같게
    public string[] sceneNames = { "BossTestScene", "FinalBossTestScene" };
    public float difficultyRampMinutes = 30f; // ZombieSpawner.difficultyRampMinutes와 같은 값(체력·공격력 배율 계산용)

    public Color selectedTabColor = new Color(0.93f, 0.74f, 0.36f, 1f);
    public Color normalTabColor = new Color(0.2f, 0.22f, 0.25f, 1f);
    public Color selectedTextColor = new Color(0.08f, 0.08f, 0.09f, 1f);
    public Color normalTextColor = new Color(0.95f, 0.95f, 0.93f, 1f);

    private int tab;
    private int characterIndex;

    public void Open() {
        panel.SetActive(true);
        characterIndex = Mathf.Max(0, System.Array.IndexOf(characterSelect.characters, CharacterSelection.selected));
        ApplyCharacter();
        SelectTab(tab);
    }

    public void Close() {
        panel.SetActive(false);
    }

    public void SelectTab(int index) {
        tab = Mathf.Clamp(index, 0, sceneNames.Length - 1);
        for (int i = 0; i < tabFrames.Length; i++)
        {
            bool selected = i == tab;
            tabFrames[i].color = selected ? selectedTabColor : normalTabColor;
            tabTexts[i].color = selected ? selectedTextColor : normalTextColor;
        }

        RefreshInfo();
    }

    public void SelectBossTab() { SelectTab(0); }
    public void SelectFinalBossTab() { SelectTab(1); }

    // 연습에 쓸 캐릭터를 A <-> B로 전환
    public void CycleCharacter() {
        characterIndex = (characterIndex + 1) % characterSelect.characters.Length;
        ApplyCharacter();
    }

    private void ApplyCharacter() {
        CharacterData data = characterSelect.characters[characterIndex];
        CharacterSelection.selected = data;
        if (characterButtonText != null)
        {
            characterButtonText.text = "PLAY AS : " + data.displayName;
        }
    }

    public void StartPractice() {
        introMenu.LoadScene(sceneNames[tab]);
    }

    private void RefreshInfo() {
        ZombieData data = tab < bossDatas.Length ? bossDatas[tab] : null;
        float minutes = tab < startMinutes.Length ? startMinutes[tab] : 0f;
        float multiplier = 1f + minutes / difficultyRampMinutes;

        if (data == null)
        {
            infoText.text = "";
            bestText.text = "";
            return;
        }

        string title = tab == 0 ? "VS BOSS" : "VS FINAL BOSS";
        string clock = Mathf.FloorToInt(minutes) + ":00";
        infoText.text = "<color=#EDBD5C>" + title + "</color>   GAME TIME " + clock
            + "\nHP " + Mathf.RoundToInt(data.health * multiplier)
            + "   DAMAGE " + Mathf.RoundToInt(data.damage * multiplier)
            + "   SPEED " + data.speed.ToString("0.0")
            + (data.rangedPattern ? "\nRED ZONE EVERY " + data.rangedInterval.ToString("0") + " S   RADIUS " + data.rangedZoneRadius.ToString("0.0") + " M" : "")
            + "\nALL WEAPONS   NO OTHER ENEMIES";

        float best = PracticeBossMode.GetBestSeconds((PracticeBossMode.Kind)tab);
        bestText.text = "BEST TIME : " + (best > 0f ? PracticeBossMode.FormatTime(best) : "--");
    }
}
