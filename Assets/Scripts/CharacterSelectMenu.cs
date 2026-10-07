using UnityEngine;
using UnityEngine.UI; // UI 관련 코드

// 인트로의 캐릭터 선택 패널: A·B 비교 표시, 선택, 선택한 캐릭터로 게임 시작
public class CharacterSelectMenu : MonoBehaviour {
    public IntroMenu introMenu; // 게임 시작(씬 전환)을 담당
    public GameObject panel; // 선택 패널 루트
    public CharacterData[] characters; // 선택 가능한 캐릭터(0=A, 1=B)
    public Image[] cardFrames; // 캐릭터별 카드 테두리(선택 표시)
    public Text[] nameTexts; // 캐릭터별 이름·역할
    public Text[] statTexts; // 캐릭터별 배율 비교
    public Image[] colorSwatches; // 캐릭터별 유니폼 색 견본

    public Color selectedFrameColor = new Color(0.93f, 0.74f, 0.36f, 1f); // 선택된 카드 테두리(앰버)
    public Color normalFrameColor = new Color(0.2f, 0.22f, 0.25f, 1f); // 선택 안 된 카드 테두리

    private int selectedIndex;
    private bool selectionRestored; // 이전 확정 캐릭터를 처음 열 때 한 번만 복원하기 위한 가드

    // 카드 문구·선택 상태 갱신(패널을 열 때마다 호출. 패널은 씬에서 비활성 상태로 시작)
    private void Refresh() {
        // 카드 문구는 데이터에서 만들어 배율 표시와 실제 적용값이 항상 같게 한다
        for (int i = 0; i < characters.Length; i++)
        {
            CharacterData data = characters[i];
            nameTexts[i].text = data.displayName + "\n<size=16>" + data.roleLabel + "</size>";
            // 항목 이름은 카드의 왼쪽 라벨 텍스트에 고정, 여기서는 오른쪽 값 열만 채운다(MOVE/FIRE RATE/MAX HP/RELOAD/DAMAGE 순)
            statTexts[i].text = FormatMultiplier(data.moveSpeedMultiplier)
                + "\n" + FormatMultiplier(data.attackSpeedMultiplier)
                + "\n" + FormatMultiplier(data.maxHealthMultiplier)
                + "\n" + FormatMultiplier(data.reloadSpeedMultiplier)
                + "\n" + FormatMultiplier(data.damageMultiplier);
            colorSwatches[i].color = data.uniformColor;
        }

        // 처음 열 때만 이전에 확정한 캐릭터를 복원하고, 이후에는 고르던 캐릭터를 유지(BACK 후 다시 열어도 선택이 A로 돌아가지 않게 함)
        if (!selectionRestored)
        {
            int previous = System.Array.IndexOf(characters, CharacterSelection.selected);
            selectedIndex = previous >= 0 ? previous : 0;
            selectionRestored = true;
        }
        Select(selectedIndex);
    }

    // 배율 표시: 1보다 크면 초록, 작으면 빨강, 같으면 기본색
    private static string FormatMultiplier(float value) {
        string text = "x" + value.ToString("0.00");
        if (value > 1.001f)
        {
            return "<color=#7CCB6B>" + text + "</color>";
        }
        if (value < 0.999f)
        {
            return "<color=#E6474D>" + text + "</color>";
        }
        return text;
    }

    // 선택 패널 열기(인트로 START 버튼)
    public void Open() {
        Refresh();
        panel.SetActive(true);
    }

    // 선택 패널 닫기(BACK 버튼)
    public void Close() {
        panel.SetActive(false);
    }

    // 카드 클릭으로 캐릭터 선택
    public void Select(int index) {
        selectedIndex = index;
        for (int i = 0; i < cardFrames.Length; i++)
        {
            cardFrames[i].color = i == index ? selectedFrameColor : normalFrameColor;
        }
    }

    // 선택한 캐릭터로 게임 시작
    public void StartSelected() {
        CharacterSelection.selected = characters[selectedIndex];
        introMenu.StartGame();
    }
}
