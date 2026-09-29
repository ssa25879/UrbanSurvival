using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 플레이어 캐릭터를 조작하기 위한 사용자 입력을 감지
// 감지된 입력값을 다른 컴포넌트들이 사용할 수 있도록 제공
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

    // 일시정지·게임오버·창 포커스 변경 직후에는 누르고 있던 발사 버튼이 다시 눌린 것으로 처리되지 않도록,
    // 발사 버튼을 한 번 뗄 때까지 발사 입력을 막는다(UI 버튼 클릭이나 창 클릭으로 돌아온 클릭이 오발이 되는 것을 방지)
    private bool suppressFireUntilRelease;
    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

    private void OnApplicationFocus(bool hasFocus) {
        suppressFireUntilRelease = true;
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
            // 정지 중에 누른 버튼(예: 계속하기 클릭)이 재개 직후 발사로 이어지지 않도록 막는다
            suppressFireUntilRelease = true;
            return;
        }

        // move에 관한 입력 감지
        move = Input.GetAxis(moveAxisName);
        // rotate에 관한 입력 감지
        rotate = Input.GetAxis(rotateAxisName);
        // fire에 관한 입력 감지
        fire = Input.GetButton(fireButtonName);
        fireDown = Input.GetButtonDown(fireButtonName);

        // 발사 차단: 정지·포커스 복귀 직후 누른 채로 남은 버튼, 화면 밖 포인터, 클릭 가능한 UI 위 포인터
        if (suppressFireUntilRelease)
        {
            if (!fire)
            {
                suppressFireUntilRelease = false;
            }
            else
            {
                fire = false;
                fireDown = false;
            }
        }

        if ((fire || fireDown) && IsPointerBlockedForFire())
        {
            fire = false;
            fireDown = false;
        }

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
}
