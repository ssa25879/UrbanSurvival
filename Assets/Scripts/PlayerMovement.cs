using UnityEngine;

// 플레이어 캐릭터를 사용자 입력에 따라 움직이는 스크립트
public class PlayerMovement : MonoBehaviour {
    public float moveSpeed = 5f; // 평면 이동 속도
    public float rotateSpeed = 180f; // 기존 직렬화 값을 보존하기 위한 회전 속도

    [SerializeField] private string param;


    private PlayerInput playerInput; // 플레이어 입력을 알려주는 컴포넌트
    private Rigidbody playerRigidbody; // 플레이어 캐릭터의 리지드바디
    private Animator playerAnimator; // 플레이어 캐릭터의 애니메이터
    private Transform movementCamera; // 이동 방향의 기준 카메라

    private void Start() {
        // 사용할 컴포넌트들의 참조를 가져오기
        playerInput = GetComponent<PlayerInput>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerAnimator = GetComponent<Animator>();
        movementCamera = Camera.main != null ? Camera.main.transform : null;

        param = playerAnimator.parameters[0].name;
    }

    // FixedUpdate는 물리 갱신 주기에 맞춰 실행됨
    private void FixedUpdate() {
        // 카메라 기준 평면 이동만 처리하며 캐릭터 회전은 변경하지 않는다.
        Vector3 moveDirection = GetMoveDirection();
        Move(moveDirection);
        
        // 좌우 및 대각선 이동도 Locomotion에 반영한다.
        playerAnimator.SetFloat(param, moveDirection.magnitude);
    }

    // 기존 Horizontal/Vertical 입력축을 좌우/앞뒤 이동으로 사용한다.
    private Vector3 GetMoveDirection()
    {
        Vector2 input = Vector2.ClampMagnitude(new Vector2(playerInput.rotate, playerInput.move), 1f);
        Vector3 forward = Vector3.forward;

        if (movementCamera != null)
        {
            forward = Vector3.ProjectOnPlane(movementCamera.forward, Vector3.up);
            // 수직 탑뷰 카메라는 화면 위쪽 방향을 이동 기준으로 사용한다.
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.ProjectOnPlane(movementCamera.up, Vector3.up);
            }
            forward.Normalize();
        }

        Vector3 right = Vector3.Cross(Vector3.up, forward);
        return right * input.x + forward * input.y;
    }

    private void Move(Vector3 moveDirection) {
        Vector3 moveDistance = moveDirection * moveSpeed * Time.fixedDeltaTime;
        playerRigidbody.MovePosition(playerRigidbody.position + moveDistance);
    }
}
