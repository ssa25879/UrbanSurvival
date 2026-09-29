﻿﻿using System;
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

    [Header("웨이브 설정 (2026-09-29 확정: 3~8초 무작위 간격, 웨이브마다 2마리씩 증가 - Inspector에서 조정 가능)")]
    public float waveIntervalMin = 3f; // 다음 웨이브까지 최소 간격(초)
    public float waveIntervalMax = 8f; // 다음 웨이브까지 최대 간격(초)
    public int baseWaveZombieCount = 4; // 1웨이브 스폰 마릿수
    public int zombieCountIncreasePerWave = 2; // 웨이브가 지날 때마다 증가하는 마릿수(예: 4,6,8,10 ...)
    public int maxConcurrentZombies = 500; // 씬에 동시에 존재하는 좀비 최대 수(2026-09-29 확정). 시체가 사라지기 전까지는 슬롯을 차지한다

    private readonly List<Zombie> spawnedZombies = new List<Zombie>(); // 살아있는 좀비와 아직 사라지지 않은 시체(동시 상한 계산용)
    private int pendingSpawns; // 상한 때문에 아직 소환하지 못하고 대기 중인 좀비 수

    private readonly float clearHealRatio = 1f / 3f; // 적 전멸 순간 회복량 = 회복 상자 효과의 1/3(초기 제안값)
    private float nextSpawnTime; // 다음 스폰 예정 시각(Time.time 기준)
    private PlayerHealth cachedPlayerHealth; // 적 전멸 시 회복시킬 대상(최초 1회 조회 후 캐시)

    private void Update() {
        // 게임 오버 상태일때는 생성하지 않음
        if (GameManager.instance != null && GameManager.instance.isGameover)
        {
            return;
        }

        // 시체가 사라져 파괴된 좀비를 제외해 현재 씬에 남은 수를 갱신
        spawnedZombies.RemoveAll(IsDestroyed);

        // waveIntervalMin~waveIntervalMax(초) 무작위 간격마다 다음 웨이브로 전환(생존 적 전멸 대기 방식 폐지)
        if (Time.time >= nextSpawnTime)
        {
            wave++;
            SpawnWave();
            nextSpawnTime = Time.time + Random.Range(waveIntervalMin, waveIntervalMax);
        }

        // 상한 때문에 밀린 좀비를 빈 슬롯이 생기는 대로 이어서 소환
        SpawnPending();

        // UI 갱신
        UpdateUI();
    }

    // 파괴된 Unity 오브젝트인지 확인(RemoveAll용, 매 프레임 델리게이트가 새로 만들어지지 않도록 메서드로 분리)
    private static bool IsDestroyed(Zombie zombie) {
        return zombie == null;
    }

    // 웨이브 정보를 UI로 표시
    private void UpdateUI() {
        // 현재 웨이브, 살아있는 적 수, 소환 대기 수 표시
        UIManager.instance.UpdateWaveText(wave, zombies.Count, pendingSpawns);
    }

    // 현재 웨이브 번호를 기준으로 이번 웨이브에 생성할 좀비 수를 계산(예: 1,2,3,4웨이브 = 4,6,8,10마리)
    private void SpawnWave() {
        int spawnCount = baseWaveZombieCount + zombieCountIncreasePerWave * (wave - 1);

        // 바로 소환하지 않고 대기열에 넣어 두면 SpawnPending이 동시 상한 안에서 소환한다
        pendingSpawns += spawnCount;
    }

    // 동시 상한(maxConcurrentZombies)에 여유가 있는 만큼 대기 중인 좀비를 소환
    private void SpawnPending() {
        while (pendingSpawns > 0 && spawnedZombies.Count < maxConcurrentZombies)
        {
            CreateZombie();
            pendingSpawns--;
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
        spawnedZombies.Add(zombie);
        
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