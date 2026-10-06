using UnityEngine;

// 플레이어 캐릭터를 사용자 입력에 따라 움직이는 스크립트
public class PlayerMovement : MonoBehaviour {
    public float moveSpeed = 5f; // 평면 이동 속도
    public float rotateSpeed = 180f; // 기존 직렬화 값을 보존하기 위한 회전 속도
    public float playAreaHalfExtent = 65f; // 플레이어 이동 제한 범위(맵은 150x150이지만 플레이어는 130x130 안에서만 이동)

    [SerializeField] private string param;


    private PlayerInput playerInput; // 플레이어 입력을 알려주는 컴포넌트
    private Rigidbody playerRigidbody; // 플레이어 캐릭터의 리지드바디
    private Animator playerAnimator; // 플레이어 캐릭터의 애니메이터
    private Transform movementCamera; // 이동 방향의 기준 카메라

    private Vector3 lastAimDirection; // 마우스 조준이 평면과 만나지 않을 때 유지할 마지막 유효 방향

    private void Start() {
        // 사용할 컴포넌트들의 참조를 가져오기
        playerInput = GetComponent<PlayerInput>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerAnimator = GetComponent<Animator>();
        movementCamera = Camera.main != null ? Camera.main.transform : null;

        param = playerAnimator.parameters[0].name;
        lastAimDirection = transform.forward;
    }

    // FixedUpdate는 물리 갱신 주기에 맞춰 실행됨
    private void FixedUpdate() {
        // 카메라 기준 평면 이동 처리
        Vector3 moveDirection = GetMoveDirection();
        Move(moveDirection);

        // 이동과 독립적으로 마우스 조준 방향에 맞춰 캐릭터를 회전시킨다.
        Vector3 aimDirection = GetAimDirection();
        Rotate(aimDirection);

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
        Vector3 targetPosition = playerRigidbody.position + moveDistance;

        // 플레이어만 130x130 범위로 이동을 제한한다(적은 150x150 맵 전체를 사용).
        targetPosition.x = Mathf.Clamp(targetPosition.x, -playAreaHalfExtent, playAreaHalfExtent);
        targetPosition.z = Mathf.Clamp(targetPosition.z, -playAreaHalfExtent, playAreaHalfExtent);

        playerRigidbody.MovePosition(targetPosition);
    }

    // 마우스 스크린 좌표를 총구 높이의 수평 평면에 투영해 조준 방향을 계산한다.
    // 평면과 만나지 않는 무효 방향에서는 마지막 유효 방향을 유지한다.
    private Vector3 GetAimDirection()
    {
        // 모바일: 터치 입력이 정한 월드 조준 방향이 있으면 마우스 Ray 투영 대신 사용(PlayerInput.aimWorldDirection)
        if (playerInput.hasAimWorldDirection)
        {
            lastAimDirection = playerInput.aimWorldDirection;
            return lastAimDirection;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            return lastAimDirection;
        }

        Plane aimPlane = new Plane(Vector3.up, transform.position);
        Ray ray = cam.ScreenPointToRay(playerInput.aimPosition);

        if (aimPlane.Raycast(ray, out float distance) && distance >= 0f)
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            Vector3 direction = hitPoint - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
            {
                lastAimDirection = direction.normalized;
            }
        }

        return lastAimDirection;
    }

    // 캐릭터를 조준 방향으로 회전시킨다. 이동 입력과 무관하게 동작한다.
    private void Rotate(Vector3 aimDirection)
    {
        if (aimDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(aimDirection, Vector3.up);
        playerRigidbody.MoveRotation(targetRotation);
    }
}
