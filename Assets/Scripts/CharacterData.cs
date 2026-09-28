using UnityEngine;

// 플레이 가능한 캐릭터(A/B)의 기준 배율 데이터. 런타임에 이 애셋을 수정하지 않고 값만 읽어서 적용한다
[CreateAssetMenu(menuName = "Scriptable/CharacterData", fileName = "Character Data")]
public class CharacterData : ScriptableObject {
    public string displayName = "CHARACTER A"; // 선택 화면 이름
    public string roleLabel = "ENDURANCE"; // 선택 화면 역할 설명

    public float moveSpeedMultiplier = 1f; // 이동속도 배율
    public float attackSpeedMultiplier = 1f; // 공격속도 배율(실제 발사 간격 = 기본 간격 / 배율)
    public float maxHealthMultiplier = 1f; // 최대 체력 배율
    public float reloadSpeedMultiplier = 1f; // 재장전속도 배율(실제 재장전 시간 = 기본 시간 / 배율)
    public float damageMultiplier = 1f; // 공격력 배율

    public Color uniformColor = Color.white; // 외형 구분용 유니폼 색
}
