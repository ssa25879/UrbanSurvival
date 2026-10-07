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
    public Zombie prefabOverride; // 이 데이터 전용 좀비 프리팹(외형·판정 크기가 다른 개체용, 비우면 ZombieSpawner.zombiePrefab 사용, 2026-10-01 추가)

    [Header("보스 (2026-09-29 확정, 수치는 초기 제안값)")]
    public bool isBoss = false; // 보스 여부(체력 UI 표시 대상)
    public string displayName = ""; // 보스 체력바에 표시할 이름
    public float modelScale = 1f; // 외형·판정 크기 배율(보스를 크게 표시)
    public int score = 100; // 처치 점수

    [Header("보스 원거리 패턴 - 레드존 (2026-10-01 추가, 수치는 초기 제안값)")]
    public bool rangedPattern = false; // 원거리 패턴(레드존) 사용 여부
    public RedZone rangedZonePrefab; // 레드존 프리팹(Assets/Prefabs/BossRedZone)
    public float rangedFirstDelay = 4f; // 등장 후 첫 패턴까지 시간(초)
    public float rangedInterval = 7f; // 패턴 간격(초)
    public float rangedWarnSeconds = 1.6f; // 레드존 경고 시간(초). 이 시간이 지나면 폭발
    public float rangedZoneRadius = 3f; // 레드존 반지름(m)
    public float rangedDamageMultiplier = 0.75f; // 레드존 피해 = 보스 현재 공격력 x 이 값
    public int rangedZoneCount = 1; // 지정 공격 한 번에 만드는 레드존 수
    public int rangedSlamEvery = 3; // 이 횟수마다 한 번은 보스 중심 내려찍기(0이면 사용 안 함)
    public float rangedPercentMaxHealth = 0f; // 0보다 크면 피해 = 플레이어 최대(전체) 체력 x 이 값(Final Boss 0.9). 0이면 공격력 x 배율
}
