using System.Collections.Generic;
using UnityEngine;

public struct AimCandidate {
    public Vector3 position;
    public bool alive;

    public AimCandidate(Vector3 position, bool alive) {
        this.position = position;
        this.alive = alive;
    }
}

public struct AimResult {
    public bool valid;      // 쓸 수 있는 방향이 있는지
    public bool hasTarget;  // 사거리 안의 적을 골랐는지
    public Vector3 direction; // y=0으로 정규화된 방향
}

// 오토 에임 대상 선택(순수 함수). 방향만 정하고 명중·산포·사거리·탄약 판정은 건드리지 않는다
public static class AutoAimTargeting {
    private const float MinSqrDistance = 0.000001f;

    // 우선순위: 1) 사거리 안 가장 가까운 살아 있는 적 2) 이동 방향 3) 마지막 유효 방향
    // 거리는 수평(XZ)으로 계산하며 사거리와 같은 거리의 적도 대상이다. range가 0 이하이면 대상이 없다
    public static AimResult Select(Vector3 origin, IReadOnlyList<AimCandidate> candidates, float range, Vector3 moveDirection, Vector3 lastDirection) {
        if (range > 0f && candidates != null)
        {
            float rangeSqr = range * range;
            float bestSqr = float.MaxValue;
            Vector3 best = Vector3.zero;
            bool found = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                AimCandidate candidate = candidates[i];
                if (!candidate.alive)
                {
                    continue;
                }

                Vector3 offset = candidate.position - origin;
                offset.y = 0f;
                float sqr = offset.sqrMagnitude;
                if (sqr < MinSqrDistance || sqr > rangeSqr || sqr >= bestSqr)
                {
                    continue;
                }

                bestSqr = sqr;
                best = offset;
                found = true;
            }

            if (found)
            {
                return new AimResult { valid = true, hasTarget = true, direction = best.normalized };
            }
        }

        Vector3 move = Flatten(moveDirection);
        if (move.sqrMagnitude > MinSqrDistance)
        {
            return new AimResult { valid = true, hasTarget = false, direction = move.normalized };
        }

        Vector3 last = Flatten(lastDirection);
        if (last.sqrMagnitude > MinSqrDistance)
        {
            return new AimResult { valid = true, hasTarget = false, direction = last.normalized };
        }

        return new AimResult { valid = false, hasTarget = false, direction = Vector3.zero };
    }

    private static Vector3 Flatten(Vector3 direction) {
        direction.y = 0f;
        return direction;
    }
}
