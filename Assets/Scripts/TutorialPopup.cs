using UnityEngine;
using UnityEngine.UI;

// 인트로의 튜토리얼 팝업(2026-10-01 추가): 한글 가이드를 보여 주고 확인을 누르면 원래 하려던 동작(캐릭터 선택·연습 패널 열기)을 이어서 실행한다.
// - showEveryTime이 꺼져 있으면 처음 한 번만 보여 준다(PlayerPrefs에 본 기록 저장)
// - showEveryTime이 켜져 있으면 매번 보여 주되, "다시 보지 않기"를 체크하고 확인하면 이후에는 건너뛴다
// 본문 글꼴은 게임 글꼴(Kenney Future Narrow)에 한글이 없어 운영체제의 한글 글꼴(맑은 고딕 등)을 동적으로 불러 쓴다
public class TutorialPopup : MonoBehaviour {
    public GameObject root; // 팝업 루트(씬에서 비활성 상태로 시작)
    public Text bodyText; // 본문(한글)
    public Text[] koreanTexts; // 한글 글꼴을 적용할 텍스트들(본문, 확인 버튼, 체크 문구 등)
    [TextArea(6, 20)] public string body; // 가이드 본문(리치 텍스트 가능)
    public string prefsKey = "TutorialSeen_Game"; // "봤음" 기록 키
    public bool showEveryTime = false; // 켜면 다시 보지 않기를 고르기 전까지 매번 표시
    public Button dontShowButton; // "다시 보지 않기" 체크 버튼(showEveryTime일 때만 보인다)
    public Text dontShowText; // 체크 버튼 문구

    private System.Action onConfirmed;
    private bool dontShowChecked;
    private static Font koreanFont;

    // 지금 팝업을 보여 줘야 하는지(처음 한 번 또는 매번 방식)
    public bool ShouldShow() {
        return PlayerPrefs.GetInt(prefsKey, 0) == 0;
    }

    // 팝업을 보여 주고 확인하면 onDone 실행. 보여 줄 필요가 없으면 바로 onDone 실행
    public void ShowThen(System.Action onDone) {
        if (!ShouldShow())
        {
            onDone?.Invoke();
            return;
        }

        onConfirmed = onDone;
        dontShowChecked = false;
        ApplyFont();
        bodyText.text = MobilePlatform.IsMobile ? MobileTutorialText.Convert(body, MobileAimSettings.Mode) : body;
        if (dontShowButton != null)
        {
            dontShowButton.gameObject.SetActive(showEveryTime);
        }
        RefreshCheckText();
        root.SetActive(true);
    }

    // 확인 버튼
    public void Confirm() {
        // 처음 한 번 방식은 확인하면 본 것으로 기록, 매번 방식은 "다시 보지 않기"를 체크했을 때만 기록
        if (!showEveryTime || dontShowChecked)
        {
            PlayerPrefs.SetInt(prefsKey, 1);
            PlayerPrefs.Save();
        }

        root.SetActive(false);
        System.Action next = onConfirmed;
        onConfirmed = null;
        next?.Invoke();
    }

    // "다시 보지 않기" 토글
    public void ToggleDontShow() {
        dontShowChecked = !dontShowChecked;
        RefreshCheckText();
    }

    private void RefreshCheckText() {
        if (dontShowText != null)
        {
            dontShowText.text = (dontShowChecked ? "[ V ]  " : "[    ]  ") + "다시 보지 않기";
        }
    }

    private void ApplyFont() {
        if (koreanFont == null)
        {
            koreanFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "NanumGothic", "Noto Sans KR", "Gulim", "AppleGothic", "Noto Sans CJK KR", "Noto Sans CJK", "sans-serif" }, 22);
        }

        if (koreanFont == null || koreanTexts == null)
        {
            return;
        }

        for (int i = 0; i < koreanTexts.Length; i++)
        {
            if (koreanTexts[i] != null)
            {
                koreanTexts[i].font = koreanFont;
            }
        }
    }
}
