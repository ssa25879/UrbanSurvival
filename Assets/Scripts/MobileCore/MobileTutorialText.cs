// 튜토리얼 본문의 PC 전용 조작 설명(키보드·마우스)을 모바일 조작 설명으로 바꾼다(순수 문자열 함수)
// 본문 전체를 다시 쓰지 않고 해당 문장만 바꿔 서식 태그와 나머지 가이드는 그대로 둔다. 줄바꿈 형식(\n, \r\n)에 상관없이 한 줄씩 바꾼다
public static class MobileTutorialText {
    private const string PcMoveLine = "WASD 이동   /   마우스 조준   /   좌클릭 발사";
    private const string PcOtherLine = "R 재장전   /   1~4 무기 교체   /   ESC 일시정지";
    private const string PcReloadTip = "R 키를 누르거나 다시 클릭해서 재장전하세요.";

    public static string Convert(string body, MobileAimMode mode) {
        if (string.IsNullOrEmpty(body))
        {
            return string.Empty;
        }

        bool twin = mode == MobileAimMode.TwinStick;
        string moveLine = twin
            ? "왼쪽 화면 드래그 이동   /   오른쪽 화면 드래그 조준·발사"
            : "왼쪽 화면 드래그 이동   /   FIRE 버튼 자동 조준 발사";
        string otherLine = "R 버튼 재장전   /   무기 칸 터치 교체   /   II 버튼 일시정지";
        string reloadTip = twin
            ? "R 버튼을 누르거나 오른쪽 화면을 다시 당겨서 재장전하세요."
            : "R 버튼을 누르거나 FIRE 버튼을 다시 눌러서 재장전하세요.";

        return body
            .Replace(PcMoveLine, moveLine)
            .Replace(PcOtherLine, otherLine)
            .Replace(PcReloadTip, reloadTip);
    }
}
