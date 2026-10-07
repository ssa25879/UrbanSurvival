// 인트로에서 고른 캐릭터를 인게임 씬으로 전달(씬 재시작 시에도 같은 캐릭터 유지)
public static class CharacterSelection {
    public static CharacterData selected; // 선택된 캐릭터(없으면 인게임의 기본 캐릭터 사용)
    public static bool openPracticeOnIntro; // 연습 씬에서 SELECT로 인트로에 온 경우, 인트로가 연습(PRACTICE) 패널을 바로 연다
    public static bool openSelectOnIntro; // 인게임에서 "선택 화면으로 돌아가기"로 인트로에 온 경우, 인트로가 캐릭터 선택 패널을 바로 연다
}
