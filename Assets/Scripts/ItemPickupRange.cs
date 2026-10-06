using UnityEngine;

// 아이템 습득 범위를 월드 기준 반경(m)으로 맞춘다(2026-10-06 사용자 요청: 보스 레드존 반경과 같은 약 3 m, 모바일·PC 공통).
// 습득 판정은 기존대로 플레이어(PlayerHealth.OnTriggerEnter)가 이 트리거에 들어올 때 일어난다.
// WeaponPickup은 Awake에서 모델 크기에 맞춰 루트 스케일을 바꾸므로, 그 뒤인 Start에서 현재 스케일로 반경을 환산한다.
// 사격 레이(Gun)는 트리거를 무시하므로 넓어진 트리거가 총알을 막지 않는다
[RequireComponent(typeof(SphereCollider))]
public class ItemPickupRange : MonoBehaviour {
    [Tooltip("아이템 중심에서 습득되는 월드 반경(m)")]
    public float pickupRadius = 3f;

    private void Start() {
        SphereCollider trigger = GetComponent<SphereCollider>();
        Vector3 scale = transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        if (maxScale <= 0.0001f)
        {
            return;
        }

        trigger.radius = pickupRadius / maxScale;
    }
}
