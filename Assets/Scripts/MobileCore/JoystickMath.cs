using UnityEngine;

// 조이스틱 입력 변환(순수 함수). 위젯과 PlayerInput이 같은 규칙을 쓰도록 한 곳에 둔다
public static class JoystickMath {
    // offset: 스틱 중심에서 손가락까지의 화면 거리(px), radius: 스틱 반지름(px)
    // 반환값은 크기 1 이하. 데드존 이하는 0, 그 위는 데드존만큼 빼고 0~1로 다시 편다
    public static Vector2 Evaluate(Vector2 offset, float radius, float deadZone) {
        if (radius <= 0f)
        {
            return Vector2.zero;
        }

        float dead = Mathf.Clamp(deadZone, 0f, 0.95f);
        Vector2 normalized = offset / radius;
        float magnitude = normalized.magnitude;
        if (magnitude <= dead || magnitude <= 0f)
        {
            return Vector2.zero;
        }

        float scaled = Mathf.Clamp01((magnitude - dead) / (1f - dead));
        return normalized / magnitude * scaled;
    }

    // 화면 기준 스틱 방향을 카메라 기준 평면(y=0) 월드 방향으로 바꾼다. PlayerMovement.GetMoveDirection과 같은 규칙
    public static Vector3 ToWorldDirection(Vector2 stick, Vector3 cameraForward, Vector3 cameraUp) {
        if (stick.sqrMagnitude <= 0f)
        {
            return Vector3.zero;
        }

        Vector3 forward = Vector3.ProjectOnPlane(cameraForward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(cameraUp, Vector3.up);
        }
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward);
        return right * stick.x + forward * stick.y;
    }
}
