using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 플레이어 캐릭터를 조작하기 위한 사용자 입력을 감지
// 감지된 입력값을 다른 컴포넌트들이 사용할 수 있도록 제공
// PC는 키보드·마우스, 모바일(MobilePlatform.IsMobile)은 터치 UI가 쓰는 MobileInputState를 읽어 같은 출력 값을 낸다
public class PlayerInput : MonoBehaviour {
    public string moveAxisName = "Vertical"; // 앞뒤 움직임을 위한 입력축 이름
    public string rotateAxisName = "Horizontal"; // 좌우 회전을 위한 입력축 이름
    public string fireButtonName = "Fire1"; // 발사를 위한 입력 버튼 이름
    public string reloadButtonName = "Reload"; // 재장전을 위한 입력 버튼 이름

    // 값 할당은 내부에서만 가능
    public float move { get; private set; } // 감지된 움직임 입력값
    public float rotate { get; private set; } // 감지된 회전 입력값
    public bool fire { get; private set; } // 감지된 발사 입력값(누르고 있는 동안 true, 연사용)
    public bool fireDown { get; private set; } // 발사 버튼을 누른 첫 프레임(단발/엣지 판정용)
    public bool reload { get; private set; } // 감지된 재장전 입력값
    public Vector2 aimPosition { get; private set; } // 감지된 마우스 조준 화면 좌표

    public bool swapToSlot1 { get; private set; } // 1번 슬롯(권총) 스왑 입력
    public bool swapToSlot2 { get; private set; } // 2번 슬롯(소총) 스왑 입력
    public bool swapToSlot3 { get; private set; } // 3번 슬롯(SMG) 스왑 입력
    public bool swapToSlot4 { get; private set; } // 4번 슬롯(산탄총) 스왑 입력

    // 모바일 조준: 유효한 월드 방향(y=0, 정규화)이 있으면 PlayerMovement가 마우스 Ray 투영 대신 이 방향을 쓴다
    public Vector3 aimWorldDirection { get; private set; }
    public bool hasAimWorldDirection { get; private set; }

    private const float AutoAimRefreshInterval = 0.1f; // 오토 에임 대상 재탐색 간격(초)

    // 일시정지·게임오버·창 포커스 변경 직후에는 누르고 있던 발사 버튼이 다시 눌린 것으로 처리되지 않도록,
    // 발사 버튼을 한 번 뗄 때까지 발사 입력을 막는다(UI 버튼 클릭이나 창 클릭으로 돌아온 클릭이 오발이 되는 것을 방지)
    private readonly FireLatch fireLatch = new FireLatch();
    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

    private readonly TwinStickFireTracker twinStickFire = new TwinStickFireTracker();
    private readonly List<AimCandidate> aimCandidates = new List<AimCandidate>(512);
    private PlayerShooter playerShooter;
    private Vector3 lastMobileAim;
    private float nextAutoAimTime;
    private readonly MobileFireLogic fireLogic = new MobileFireLogic(); // 모바일 발사·재장전 결정(순수 로직, 테스트 있음)

    private void Start() {
        playerShooter = GetComponent<PlayerShooter>();
        lastMobileAim = transform.forward;
        lastMobileAim.y = 0f;
        lastMobileAim = lastMobileAim.sqrMagnitude > 0.0001f ? lastMobileAim.normalized : Vector3.forward;
    }

    private void OnApplicationFocus(bool hasFocus) {
        fireLatch.RequireRelease();
        MobileInputState.ResetAll();
        twinStickFire.Reset();
        fireLogic.Reset();
    }

    private void OnApplicationPause(bool paused) {
        fireLatch.RequireRelease();
        MobileInputState.ResetAll();
        twinStickFire.Reset();
        fireLogic.Reset();
    }

    // 조준 모드 전환 등으로 눌려 있던 발사 입력이 그대로 발사로 이어지지 않도록, 다음 발사를 한 번 뗀 뒤로 미룬다
    public void RequireFireRelease() {
        fireLatch.RequireRelease();
        MobileInputState.ResetAll();
        twinStickFire.Reset();
        fireLogic.Reset();
    }

    // 마우스 포인터가 게임 화면 밖이거나, 클릭 가능한 UI(버튼 등) 위에 있는지 확인
    private bool IsPointerBlockedForFire() {
        Vector3 pointer = Input.mousePosition;
        if (pointer.x < 0f || pointer.y < 0f || pointer.x > Screen.width || pointer.y > Screen.height)
        {
            return true;
        }

        if (EventSystem.current == null)
        {
            return false;
        }

        // 클릭할 수 없는 HUD 패널 위에서는 사격이 막히지 않도록, 버튼 같은 Selectable UI만 검사한다
        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = pointer };
        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, uiRaycastResults);
        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            if (uiRaycastResults[i].gameObject.GetComponentInParent<Selectable>() != null)
            {
                return true;
            }
        }

        return false;
    }

    // 매프레임 사용자 입력을 감지
    private void Update() {
        // 게임오버·일시정지 상태에서는 사용자 입력을 감지하지 않는다
        // (일시정지는 Time.timeScale=0이라 발사 쿨타임 등은 자연히 멈추지만, Update() 자체는 계속 돌기 때문에
        // 이 가드가 없으면 일시정지 중에도 새로 누른 입력이 그대로 통과해 총이 나가는 문제가 있었음)
        if (GameManager.instance && (GameManager.instance.isGameover || GameManager.instance.isPaused))
        {
            move = 0;
            rotate = 0;
            fire = false;
            fireDown = false;
            reload = false;
            swapToSlot1 = false;
            swapToSlot2 = false;
            swapToSlot3 = false;
            swapToSlot4 = false;
            hasAimWorldDirection = false;
            MobileInputState.ResetAll();
            twinStickFire.Reset();
            fireLogic.Reset();
            // 정지 중에 누른 버튼(예: 계속하기 클릭)이 재개 직후 발사로 이어지지 않도록 막는다
            fireLatch.RequireRelease();
            return;
        }

        bool mobile = MobilePlatform.IsMobile;
        if (mobile)
        {
            ReadMobileInput();
        }
        else
        {
            hasAimWorldDirection = false;
            ReadDesktopInput();
        }

        // 발사 차단: 정지·포커스 복귀 직후 누른 채로 남은 버튼, 화면 밖 포인터, 클릭 가능한 UI 위 포인터
        bool latchedFire = fire;
        bool latchedFireDown = fireDown;
        fireLatch.Apply(ref latchedFire, ref latchedFireDown);
        fire = latchedFire;
        fireDown = latchedFireDown;

        // 마우스 포인터 검사는 PC 전용(터치의 발사 버튼은 그 자체가 UI라 이 검사를 거치면 항상 막힌다)
        if (!mobile && (fire || fireDown) && IsPointerBlockedForFire())
        {
            fire = false;
            fireDown = false;
        }
    }

    // PC: 키보드·마우스(기존 동작 그대로)
    private void ReadDesktopInput() {
        // move에 관한 입력 감지
        move = Input.GetAxis(moveAxisName);
        // rotate에 관한 입력 감지
        rotate = Input.GetAxis(rotateAxisName);
        // fire에 관한 입력 감지
        fire = Input.GetButton(fireButtonName);
        fireDown = Input.GetButtonDown(fireButtonName);

        // reload에 관한 입력 감지
        reload = Input.GetButtonDown(reloadButtonName);
        // 마우스 조준 위치 감지(이동과 독립적으로 처리)
        aimPosition = Input.mousePosition;

        // 무기 슬롯 스왑 입력(1=권총, 2=소총, 3=SMG, 4=산탄총)
        swapToSlot1 = Input.GetKeyDown(KeyCode.Alpha1);
        swapToSlot2 = Input.GetKeyDown(KeyCode.Alpha2);
        swapToSlot3 = Input.GetKeyDown(KeyCode.Alpha3);
        swapToSlot4 = Input.GetKeyDown(KeyCode.Alpha4);
    }

    // 모바일: 터치 UI가 쓴 MobileInputState를 읽는다(마우스 좌표는 쓰지 않는다)
    private void ReadMobileInput() {
        Vector2 moveStick = MobileInputState.Move;
        rotate = moveStick.x;
        move = moveStick.y;
        aimPosition = Vector2.zero;

        reload = MobileInputState.ConsumeReload();
        int slot = MobileInputState.ConsumeSwap();
        swapToSlot1 = slot == 0;
        swapToSlot2 = slot == 1;
        swapToSlot3 = slot == 2;
        swapToSlot4 = slot == 3;

        Camera cam = Camera.main;
        Vector3 camForward = cam != null ? cam.transform.forward : Vector3.forward;
        Vector3 camUp = cam != null ? cam.transform.up : Vector3.up;

        bool twin = MobileAimSettings.Mode == MobileAimMode.TwinStick;
        bool held;
        bool down;

        if (twin)
        {
            Vector2 aimStick = MobileInputState.AimStick;
            twinStickFire.Update(aimStick);
            held = twinStickFire.Held;
            down = twinStickFire.Down;
            MobileInputState.ConsumeFireDown(); // 이 모드에서는 발사 버튼 요청을 쓰지 않는다

            if (held)
            {
                Vector3 direction = JoystickMath.ToWorldDirection(aimStick, camForward, camUp);
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    lastMobileAim = direction.normalized;
                }
            }
        }
        else
        {
            twinStickFire.Reset();
            bool pressed = MobileInputState.ConsumeFireDown();
            held = MobileInputState.FireHeld || pressed;
            down = pressed;

            // 발사 버튼을 누르는 동안만 대상을 찾는다(버튼을 떼면 마지막 방향 유지)
            if (held && (pressed || Time.time >= nextAutoAimTime))
            {
                nextAutoAimTime = Time.time + AutoAimRefreshInterval;
                lastMobileAim = ResolveAutoAim(moveStick, camForward, camUp);
            }
        }

        // 발사·재장전 결정(누른 채 빈 탄창 자동 재장전·재개, 단발 무기 자동 반복)은 순수 로직(MobileFireLogic)이 맡는다
        Gun currentGun = playerShooter != null ? playerShooter.gun : null;
        MobileFireResult decision = fireLogic.Evaluate(new MobileFireFrame
        {
            held = held,
            down = down,
            reloadRequested = reload,
            gunState = ToMobileGunState(currentGun),
            gunIsAutomatic = currentGun != null && currentGun.gunData != null
                && currentGun.gunData.fireMode == GunData.FireMode.Automatic
        });
        fire = decision.fire;
        fireDown = decision.fireDown;
        reload = decision.reload;
        aimWorldDirection = lastMobileAim;
        hasAimWorldDirection = true;
    }

    // Gun의 상태를 MobileCore의 열거형으로 바꾼다(MobileCore는 게임 코드에 의존하지 않는다)
    private static MobileGunState ToMobileGunState(Gun gun) {
        if (gun == null || gun.gunData == null)
        {
            return MobileGunState.None;
        }

        switch (gun.state)
        {
            case Gun.State.Ready: return MobileGunState.Ready;
            case Gun.State.Empty: return MobileGunState.Empty;
            case Gun.State.Reloading: return MobileGunState.Reloading;
            default: return MobileGunState.None;
        }
    }
    // 사거리 안 가장 가까운 적 → 이동 방향 → 마지막 방향 순으로 조준 방향을 정한다
    private Vector3 ResolveAutoAim(Vector2 moveStick, Vector3 camForward, Vector3 camUp) {
        aimCandidates.Clear();
        for (int i = 0; i < Zombie.alive.Count; i++)
        {
            Zombie zombie = Zombie.alive[i];
            if (zombie != null)
            {
                aimCandidates.Add(new AimCandidate(zombie.transform.position, !zombie.dead));
            }
        }

        float range = 0f;
        if (playerShooter != null && playerShooter.gun != null && playerShooter.gun.gunData != null)
        {
            range = playerShooter.gun.gunData.range;
        }

        Vector3 moveDirection = JoystickMath.ToWorldDirection(moveStick, camForward, camUp);
        AimResult result = AutoAimTargeting.Select(transform.position, aimCandidates, range, moveDirection, lastMobileAim);
        return result.valid ? result.direction : lastMobileAim;
    }
}