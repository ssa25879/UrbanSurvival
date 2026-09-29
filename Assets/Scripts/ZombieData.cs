using UnityEngine;

// 좀비 생성시 사용할 셋업 데이터
[CreateAssetMenu(menuName = "Scriptable/ZombieData", fileName = "Zombie Data")]
public class ZombieData : ScriptableObject {
    public float health = 100f; // 체력
    public float damage = 20f; // 공격력
    public float speed = 2f; // 이동 속도
    public Color skinColor = Color.white; // 피부색(몸 재질의 원래 색에 곱함, 흰색이면 원래 외형 유지, 어둡게 만들 때 사용)
    public Color glowColor = Color.black; // 몸에 더하는 발광색(붉은 기운 등, 검정이면 없음)
    public bool isElite = false; // 강화 개체 여부(미니맵 등 UI에서 일반 개체와 구분 표시)

    [Header("보스 (2026-09-29 확정, 수치는 초기 제안값)")]
    public bool isBoss = false; // 보스 여부(체력 UI 표시 대상)
    public string displayName = ""; // 보스 체력바에 표시할 이름
    public float modelScale = 1f; // 외형·판정 크기 배율(보스를 크게 표시)
    public int score = 100; // 처치 점수
}
