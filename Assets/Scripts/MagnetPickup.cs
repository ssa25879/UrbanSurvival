using UnityEngine;

// 자석 아이템(2026-10-08, 1.0.3): 획득하면 일정 시간 동안 아이템 습득 범위가 넓어져 근처 아이템이 모두 끌려온다.
// 버프 관리는 플레이어에 런타임으로 붙는 MagnetBuff가 한다(씬·플레이어 프리팹을 수정하지 않기 위함)
public class MagnetPickup : MonoBehaviour, IItem {
    [Tooltip("버프 시작 시 플레이어 발밑에 그릴 범위 원 스프라이트(링)")]
    public Sprite rangeRingSprite;

    [Tooltip("HUD에 표시할 자석 아이콘(흰색, 색은 코드에서 입힘)")]
    public Sprite hudIconSprite;

    public void Use(GameObject target) {
        MagnetBuff buff = MagnetBuff.GetOrAdd(target);
        if (buff != null)
        {
            buff.Activate(rangeRingSprite, hudIconSprite);
        }

        // 사용되었으므로, 자신을 파괴
        Destroy(gameObject);
    }
}
