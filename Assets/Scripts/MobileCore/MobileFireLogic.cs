// 모바일 발사 결정 로직(순수 클래스). PlayerInput이 프레임마다 입력을 넘기면 fire/fireDown/reload를 돌려준다.
// 게임 코드(Gun 등)에 의존하지 않도록 총 상태는 이 어셈블리의 열거형으로 받는다.

public enum MobileGunState {
    None,       // 총 없음
    Ready,      // 발사 가능
    Empty,      // 탄창이 빔
    Reloading   // 재장전 중
}

public struct MobileFireFrame {
    public bool held;            // 발사 입력이 눌려 있음(오토 에임: 버튼 누름 또는 방금 눌림, 쌍둥이 스틱: 스틱 당김)
    public bool down;            // 방금 새로 눌림
    public bool reloadRequested; // 재장전 버튼 요청
    public MobileGunState gunState;
    public bool gunIsAutomatic;  // 연사 무기(소총·SMG). false면 단발(Manual: 권총·샷건)
}

public struct MobileFireResult {
    public bool fire;
    public bool fireDown;
    public bool reload;
}

public sealed class MobileFireLogic {
    private bool resumeAfterReload; // 발사 입력을 누른 채 재장전에 들어갔다(끝나면 바로 다시 발사)

    public MobileFireResult Evaluate(MobileFireFrame frame) {
        MobileFireResult result = new MobileFireResult {
            fire = frame.held,
            fireDown = frame.down,
            reload = frame.reloadRequested
        };

        // 발사 버튼(오토 에임)이나 조준 스틱(쌍둥이 스틱)을 누른 채 탄창이 비면 자동으로 재장전한다(모바일 전체,
        // 쌍둥이 스틱은 2026-10-06, 오토 에임은 같은 날 사용자 요청으로 확대). PC(좌클릭)는 기존 규칙(자동 재장전 없음)을 따른다.
        // 재장전할 수 없으면(예비탄 없음) PlayerShooter가 무시한다
        if (frame.held && frame.gunState == MobileGunState.Empty)
        {
            result.reload = true;
        }

        // 재장전 중에도 계속 누르고 있었다면 재장전이 끝나는 즉시 다시 발사한다.
        // 재장전 중에는 발사 입력을 잠시 거두어(fire=false) PlayerShooter의 "재장전 뒤 한 번 놓아야 발사" 대기를 풀고,
        // 끝난 첫 프레임에 fireDown을 한 번 내서 단발 무기도 한 발이 나가게 한다. 손을 떼면 재개하지 않는다
        if (frame.held && frame.gunState == MobileGunState.Reloading)
        {
            resumeAfterReload = true;
            result.fire = false;
            result.fireDown = false;
        }
        else if (resumeAfterReload)
        {
            resumeAfterReload = false;
            if (frame.held)
            {
                result.fire = true;
                result.fireDown = true;
            }
        }

        // 모바일: 권총·샷건 같은 단발(Manual) 무기도 발사 버튼/조준 스틱을 누르고 있으면 자동으로 계속 발사한다(2026-10-06 사용자 요청).
        // 실제 발사 간격은 Gun이 제한한다. 발사할 수 있는 상태(Ready)일 때만 낸다(빈 탄창의 재장전은 위에서 reload로 처리)
        if (result.fire && frame.gunState == MobileGunState.Ready && !frame.gunIsAutomatic)
        {
            result.fireDown = true;
        }

        return result;
    }

    // 정지·포커스 변경·조준 모드 전환 때 남은 상태를 지운다(PlayerInput.RequireFireRelease 등이 호출)
    public void Reset() {
        resumeAfterReload = false;
    }
}

// 정지·게임 오버·창 포커스 변경·조준 모드 전환 직후에는 누르고 있던 발사 버튼이 다시 눌린 것으로 처리되지 않도록,
// 발사 입력을 한 번 뗄 때까지 막는 래치
public sealed class FireLatch {
    public bool Suppressing { get; private set; }

    public void RequireRelease() {
        Suppressing = true;
    }

    public void Apply(ref bool fire, ref bool fireDown) {
        if (!Suppressing)
        {
            return;
        }

        if (!fire)
        {
            Suppressing = false;
        }
        else
        {
            fire = false;
            fireDown = false;
        }
    }
}
