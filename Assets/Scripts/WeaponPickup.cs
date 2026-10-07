using UnityEngine;

// 처치 드랍으로 획득하는 무기 습득 아이템(소총/SMG/산탄총 전용, 권총은 항상 보유하므로 대상 아님)
public class WeaponPickup : MonoBehaviour, IItem {
    public PlayerShooter.WeaponSlot slot; // 이 아이템이 해금할 무기 슬롯

    [Tooltip("바닥에 놓인 무기 모델의 목표 최대 길이(m). 플레이어 손에 붙어있을 때 기준으로 만든 프리팹이라 원본 스케일이 부모 체인 보정값을 잃어버려 크기가 어긋나므로, 실제 렌더러 바운드를 측정해 이 길이로 정규화한다")]
    public float targetVisualLength = 0.9f;

    private void Awake() {
        NormalizeVisualScale();
    }

    // 자식 렌더러들의 실제 월드 바운드를 측정해 목표 길이에 맞춰 루트 스케일을 보정
    private void NormalizeVisualScale() {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxDimension <= 0.0001f) return;

        float scaleFactor = targetVisualLength / maxDimension;
        transform.localScale *= scaleFactor;
    }

    public void Use(GameObject target) {
        // 전달 받은 게임 오브젝트로부터 PlayerShooter 컴포넌트를 가져오기 시도
        PlayerShooter playerShooter = target.GetComponent<PlayerShooter>();

        if (playerShooter != null)
        {
            // 해당 슬롯을 해금(이미 보유 중이면 상태 유지, 처음 획득이면 기본 탄약으로 초기화)
            playerShooter.UnlockWeapon(slot);
        }

        // 사용되었으므로, 자신을 파괴
        Destroy(gameObject);
    }
}
