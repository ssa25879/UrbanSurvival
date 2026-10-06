using UnityEngine;

// 터치 UI가 값을 쓰고 PlayerInput이 읽는 정적 브리지. 게임 코드에 의존하지 않는다.
// 일회성 요청(FireDown·Reload·Swap)은 Consume으로 읽으면 바로 초기화되어 다음 프레임까지 남지 않는다.
public static class MobileInputState {
    public static Vector2 Move;      // 이동 스틱 출력(크기 1 이하, 데드존 적용 후)
    public static Vector2 AimStick;  // 쌍둥이 스틱 모드의 조준 스틱 출력(크기 1 이하, 데드존 적용 후)
    public static bool FireHeld;     // 오토 에임 모드의 발사 버튼을 누르고 있는 동안 true

    private static bool fireDownRequested;
    private static bool reloadRequested;
    private static int swapRequested = -1;

    public static void RequestFireDown() {
        fireDownRequested = true;
    }

    public static void RequestReload() {
        reloadRequested = true;
    }

    // 슬롯 인덱스 0~3(권총, 소총, SMG, 산탄총). 범위 밖 값은 무시한다
    public static void RequestSwap(int slotIndex) {
        if (slotIndex >= 0 && slotIndex <= 3)
        {
            swapRequested = slotIndex;
        }
    }

    public static bool ConsumeFireDown() {
        bool value = fireDownRequested;
        fireDownRequested = false;
        return value;
    }

    public static bool ConsumeReload() {
        bool value = reloadRequested;
        reloadRequested = false;
        return value;
    }

    // 요청이 없으면 -1
    public static int ConsumeSwap() {
        int value = swapRequested;
        swapRequested = -1;
        return value;
    }

    // 씬 전환·일시정지·포커스 변경·조준 모드 전환 때 모든 터치 입력 상태를 초기화한다
    public static void ResetAll() {
        Move = Vector2.zero;
        AimStick = Vector2.zero;
        FireHeld = false;
        fireDownRequested = false;
        reloadRequested = false;
        swapRequested = -1;
    }

    // 도메인 리로드를 끈 에디터 설정에서도 플레이 시작 때 상태가 남지 않게 한다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() {
        ResetAll();
    }
}
