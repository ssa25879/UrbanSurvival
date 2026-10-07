using NUnit.Framework;

public class MobileTutorialTextTests {
    // 인트로 튜토리얼(TutorialSeen_Game)의 실제 본문에서 PC 전용 부분만 발췌
    private const string PcControls = "WASD 이동   /   마우스 조준   /   좌클릭 발사\nR 재장전   /   1~4 무기 교체   /   ESC 일시정지";
    private const string PcTip = "R 키를 누르거나 다시 클릭해서 재장전하세요.";

    private static string Body() {
        return "<color=#EDBD5C>조작</color>\n" + PcControls + "\n\n<color=#EDBD5C>팁</color>\n탄창이 비어도 자동으로 재장전되지 않습니다. " + PcTip;
    }

    [Test]
    public void AutoAim_ReplacesPcControlsAndTip() {
        string result = MobileTutorialText.Convert(Body(), MobileAimMode.AutoAim);

        StringAssert.DoesNotContain("WASD", result);
        StringAssert.DoesNotContain("마우스", result);
        StringAssert.DoesNotContain("좌클릭", result);
        StringAssert.DoesNotContain("ESC", result);
        StringAssert.DoesNotContain("R 키", result);
        StringAssert.Contains("FIRE", result);
        StringAssert.Contains("<color=#EDBD5C>조작</color>", result, "서식 태그는 유지된다");
    }

    [Test]
    public void TwinStick_MentionsAimStickInsteadOfFireButton() {
        string result = MobileTutorialText.Convert(Body(), MobileAimMode.TwinStick);

        StringAssert.Contains("오른쪽 화면", result);
        StringAssert.DoesNotContain("FIRE 버튼", result);
        StringAssert.DoesNotContain("WASD", result);
    }

    [Test]
    public void BodyWithoutPcText_IsReturnedUnchanged() {
        const string practice = "<color=#EDBD5C>연습 모드란?</color>\n보스 한 마리와 일대일로 싸우는 모드입니다.";
        Assert.AreEqual(practice, MobileTutorialText.Convert(practice, MobileAimMode.AutoAim));
    }

    [Test]
    public void NullOrEmpty_AreSafe() {
        Assert.AreEqual(string.Empty, MobileTutorialText.Convert(null, MobileAimMode.AutoAim));
        Assert.AreEqual(string.Empty, MobileTutorialText.Convert(string.Empty, MobileAimMode.TwinStick));
    }

    [Test]
    public void WindowsLineBreaks_StillConverted() {
        string body = Body().Replace("\n", "\r\n");
        string result = MobileTutorialText.Convert(body, MobileAimMode.AutoAim);
        StringAssert.DoesNotContain("WASD", result);
    }
}
