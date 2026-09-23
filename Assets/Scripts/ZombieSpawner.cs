using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// 좀비 게임 오브젝트를 주기적으로 생성
public class ZombieSpawner : MonoBehaviour {
    public Zombie zombiePrefab; // 생성할 좀비 원본 프리팹

    public ZombieData[] zombieDatas; // 사용할 좀비 셋업 데이터들
    public Transform[] spawnPoints; // 좀비 AI를 소환할 위치들

    [Header("DropTable Prefabs")]
    public GameObject riflePickupPrefab;
    public GameObject smgPickupPrefab;
    public GameObject shotgunPickupPrefab;
    public GameObject ammoPackPrefab;
    public GameObject healthPackPrefab;

    private List<Zombie> zombies = new List<Zombie>(); // 생성된 좀비들을 담는 리스트
    private int wave; // 현재 웨이브

    private readonly int zombieScore = 100;
    private readonly float despawnTime = 10.0f;
    private readonly float lootDespawnTime = 30.0f; // 드랍 아이템 미수거 시 자동 소멸 시간(초). 무한 모드에서 미수거 드랍이 끝없이 누적되는 것을 방지
    private readonly float lootScatterRadius = 0.5f; // 같은 위치에서 연속으로 드랍될 때 겹치지 않도록 흩뿌리는 반경
    private readonly float difficultyRampMinutes = 30f; // 0분 100% -> 30분 200%, 이후도 동일한 분당 증가율로 상한 없이 계속 상승

    // 생존 적 전멸 대기 방식을 폐지하고 20~30초(무작위) 간격으로 순차 생성(초기 제안값)
    // 원문 기획서의 웨이브 1~5 구성 표(일반/강화 비율, 동시 상한, 생성 간격) 수치는 이 세션에서 확인하지 못해
    // 기존 "wave * 1.5" 공식을 분당 등장 마릿수의 임시 기준선(baseZombiesPerMinute)으로 유지함 - 원문 확인 후 교체 필요
    private readonly float minSpawnInterval = 20f;
    private readonly float maxSpawnInterval = 30f;
    private readonly float baseZombiesPerMinute = 9f; // wave*1.5 공식의 초반 평균값을 참고한 임시 기준선(미확인, 원문 표로 교체 필요)
    private readonly float zombiesPerMinuteGrowth = 15f; // 총 등장 마릿수 분당 +15마리 누적 증가(초기 제안값)
    private readonly float clearHealRatio = 1f / 3f; // 적 전멸 순간 회복량 = 회복 상자 효과의 1/3(초기 제안값)
    private float nextSpawnTime; // 다음 스폰 예정 시각(Time.time 기준)
    private PlayerHealth cachedPlayerHealth; // 적 전멸 시 회복시킬 대상(최초 1회 조회 후 캐시)

    private void Update() {
        // 게임 오버 상태일때는 생성하지 않음
        if (GameManager.instance != null && GameManager.instance.isGameover)
        {
            return;
        }

        // 20~30초(무작위) 간격으로 순차 생성(생존 적 전멸 대기 방식 폐지)
        if (Time.time >= nextSpawnTime)
        {
            float interval = Random.Range(minSpawnInterval, maxSpawnInterval);
            SpawnBatch(interval);
            nextSpawnTime = Time.time + interval;
        }

        // UI 갱신
        UpdateUI();
    }

    // 웨이브 정보를 UI로 표시
    private void UpdateUI() {
        // 현재 웨이브와 남은 적 수 표시
        UIManager.instance.UpdateWaveText(wave, zombies.Count);
    }

    // 경과 시간에 비례해 늘어나는 분당 등장 마릿수를 기준으로 이번 간격 동안 생성할 좀비 수를 계산
    private void SpawnBatch(float intervalSeconds) {
        // 스폰 회차 카운터 증가(UI 표시용, 더 이상 "전멸 후 다음 웨이브" 의미는 아님)
        wave++;

        float elapsedMinutes = GameManager.instance != null ? GameManager.instance.elapsedMinutes : 0f;
        float zombiesPerMinute = baseZombiesPerMinute + zombiesPerMinuteGrowth * elapsedMinutes;
        int spawnCount = Mathf.Max(1, Mathf.RoundToInt(zombiesPerMinute * (intervalSeconds / 60f)));

        for (int i = 0; i < spawnCount; i++)
        {
            CreateZombie();
        }
    }

    // 좀비를 생성하고 생성한 좀비에게 추적할 대상을 할당
    private void CreateZombie() {
        // 사용할 좀비 데이터를 랜덤으로 결정
        ZombieData zombieData = zombieDatas[Random.Range(0, zombieDatas.Length)];
        
        // 생성 위치 랜덤 설정
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        
        // 프리팹으로 좀비 생성
        Zombie zombie = Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);
        
        // 생성 시점의 생존 경과 시간 기준으로 시간비례 난이도 배율 계산(신규 생성분에만 적용, 이미 생성된 적에는 소급 적용하지 않음)
        float elapsedMinutes = GameManager.instance != null ? GameManager.instance.elapsedMinutes : 0f;
        float statMultiplier = 1f + (elapsedMinutes / difficultyRampMinutes);

        // 생성한 좀비에 zombieData와 난이도 배율을 할당하고 리스트에 추가
        zombie.Setup(zombieData, statMultiplier);
        zombies.Add(zombie);
        
        // onDeath 이벤트에 메서드 등록 - 리스트에서 제거, 화면에서 제거, 점수 증가, 드랍 판정, 전멸 여부 확인
        // (zombies.Remove가 먼저 실행되어야 아래 전멸 판정이 갱신된 카운트를 보고 판단할 수 있음)
        zombie.onDeath += () => zombies.Remove(zombie);
        zombie.onDeath += () => Destroy(zombie.gameObject, despawnTime);
        zombie.onDeath += () => GameManager.instance.AddScore(zombieScore);
        zombie.onDeath += () => DropLoot(zombie.transform.position);
        zombie.onDeath += HealPlayerIfAllCleared;
    }

    // 화면의 적이 모두 사라진 순간 플레이어 체력을 소폭 회복(회복 상자 효과의 1/3, 초기 제안값)
    private void HealPlayerIfAllCleared() {
        if (zombies.Count > 0)
        {
            return;
        }

        if (cachedPlayerHealth == null)
        {
            cachedPlayerHealth = FindObjectOfType<PlayerHealth>();
        }

        if (cachedPlayerHealth == null || healthPackPrefab == null)
        {
            return;
        }

        HealthPack healthPack = healthPackPrefab.GetComponent<HealthPack>();
        if (healthPack != null)
        {
            cachedPlayerHealth.RestoreHealth(healthPack.health * clearHealRatio);
        }
    }

    // 처치 시 드랍 테이블 판정: 소총10% / SMG15% / 산탄총15% / 탄약20% / 회복상자20% / 꽝(드랍없음)20%
    private void DropLoot(Vector3 position) {
        float roll = Random.Range(0f, 100f);
        GameObject dropPrefab = null;

        if (roll < 10f) dropPrefab = riflePickupPrefab;
        else if (roll < 25f) dropPrefab = smgPickupPrefab;
        else if (roll < 40f) dropPrefab = shotgunPickupPrefab;
        else if (roll < 60f) dropPrefab = ammoPackPrefab;
        else if (roll < 80f) dropPrefab = healthPackPrefab;
        // 80 이상(20%): 꽝, 드랍 없음

        if (dropPrefab != null)
        {
            // 같은 지점에서 연속으로 처치될 경우 드랍이 완전히 겹쳐 하나의 덩어리로 보이는 것을 방지
            Vector2 scatter = Random.insideUnitCircle * lootScatterRadius;
            Vector3 dropPosition = position + Vector3.up * 0.5f + new Vector3(scatter.x, 0f, scatter.y);

            GameObject drop = Instantiate(dropPrefab, dropPosition, Quaternion.identity);
            // 플레이어가 회수하지 않고 방치해도 무한 모드에서 드랍이 끝없이 쌓이지 않도록 일정 시간 후 자동 소멸
            Destroy(drop, lootDespawnTime);
        }
    }
}