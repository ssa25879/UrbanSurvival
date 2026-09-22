using UnityEngine;

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

    // 매프레임 사용자 입력을 감지
    private void Update() {
        // 게임오버 상태에서는 사용자 입력을 감지하지 않는다
        if (GameManager.instance && GameManager.instance.isGameover)
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
            return;
        }

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
}
