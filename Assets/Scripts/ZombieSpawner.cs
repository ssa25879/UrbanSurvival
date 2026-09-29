using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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

    private readonly float despawnTime = 10.0f;
    private readonly float lootDespawnTime = 30.0f; // 드랍 아이템 미수거 시 자동 소멸 시간(초). 무한 모드에서 미수거 드랍이 끝없이 누적되는 것을 방지
    private readonly float lootScatterRadius = 0.5f; // 같은 위치에서 연속으로 드랍될 때 겹치지 않도록 흩뿌리는 반경
    private readonly float difficultyRampMinutes = 30f; // 0분 100% -> 30분 200%, 이후도 동일한 분당 증가율로 상한 없이 계속 상승

    [Header("웨이브 설정 (2026-09-29 확정: 3~8초 무작위 간격, 웨이브마다 2마리씩 증가 - Inspector에서 조정 가능)")]
    public float waveIntervalMin = 3f; // 다음 웨이브까지 최소 간격(초)
    public float waveIntervalMax = 8f; // 다음 웨이브까지 최대 간격(초)
    public int baseWaveZombieCount = 4; // 1웨이브 스폰 마릿수
    public int zombieCountIncreasePerWave = 2; // 웨이브가 지날 때마다 증가하는 마릿수(예: 4,6,8,10 ...)
    public int eliteWaveInterval = 10; // 강화 적이 나오는 웨이브 간격(10, 20, 30웨이브 ... 2026-09-29 확정). 그 외 웨이브에는 일반 적(강화가 아닌 데이터)만 나온다
    public float prepareSeconds = 5f; // 게임 시작 후 첫 웨이브까지의 준비 시간(초, 기획서 5장 "5초 준비"). 준비 중에도 플레이어는 조작할 수 있다
    public float eliteRatio = 0.2f; // 강화 웨이브에서 그 웨이브 소환 마릿수 중 강화 적의 비율(초기 제안값, 최소 1마리)
    public int maxConcurrentZombies = 500; // 씬에 동시에 존재하는 좀비 최대 수(2026-09-29 확정). 시체가 사라지기 전까지는 슬롯을 차지한다

    [Header("스폰 위치 검증 (기획서 10·11장)")]
    public float minSpawnDistanceFromPlayer = 12f; // 플레이어에서 이 거리 이상 떨어진 스폰 지점만 사용
    public float spawnPointSampleDistance = 2f; // 스폰 지점에서 NavMesh 위 위치를 찾는 최대 거리
    public int spawnCandidateTries = 10; // 한 번의 소환에서 스폰 지점 후보를 뽑아 보는 횟수
    public float spawnRetryDelay = 0.5f; // 후보가 모두 실패했을 때 다시 시도하기까지의 대기 시간(초)
    public float spawnFailureLimit = 10f; // 이 시간 동안 계속 실패하면 오류로 기록(초)

    private readonly float spawnPointCheckInterval = 1f; // 스폰 지점 경로 검증 결과를 재사용하는 시간(경로 계산 횟수 절약)
    private float[] spawnPointCheckedAt; // 스폰 지점별 마지막 검증 시각
    private bool[] spawnPointUsable; // 스폰 지점별 검증 결과
    private Vector3[] spawnPointNavPosition; // 스폰 지점별 NavMesh 위 위치
    private float spawnRetryTime; // 다음 소환 시도 가능 시각
    private float spawnFailureStartTime = -1f; // 연속 실패가 시작된 시각(-1이면 실패 중이 아님)
    private bool spawnFailureLogged; // 연속 실패 오류를 이미 기록했는지
    private float lastRelocateFailureLogTime = -100f; // 재배치 실패 로그 도배 방지

    [Header("보스 (2026-09-29 확정: 10분마다 1마리, 30분 보스 처치 시 1차 목표 달성)")]
    public ZombieData bossData; // 30분 이전(10분·20분) 보스
    public ZombieData finalBossData; // 30분 이후 보스(30분 보스와 이후 10분마다 등장하는 보스)
    public float bossIntervalMinutes = 10f; // 보스 등장 간격(분)

    private int bossesScheduled; // 지금까지 등장 시각이 지난 보스 수
    private readonly Queue<int> pendingBossMinutes = new Queue<int>(); // 소환 지점을 찾지 못해 대기 중인 보스(등장 분)

    private readonly List<Zombie> spawnedZombies = new List<Zombie>(); // 살아있는 좀비와 아직 사라지지 않은 시체(동시 상한 계산용)
    private int pendingSpawns; // 상한 때문에 아직 소환하지 못하고 대기 중인 일반 좀비 수
    private int pendingEliteSpawns; // 상한 때문에 아직 소환하지 못하고 대기 중인 강화 적 수(일반 좀비보다 먼저 소환)
    private ZombieData[] normalDatas; // zombieDatas 중 강화가 아닌 데이터
    private ZombieData[] eliteDatas; // zombieDatas 중 강화 데이터

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

        // 준비 시간: 첫 웨이브 전에는 소환하지 않고 남은 초를 표시(경과 시간은 일시정지 중 흐르지 않는 씬 시작 기준 시간)
        if (Time.timeSinceLevelLoad < prepareSeconds)
        {
            UIManager.instance.UpdatePrepareText(Mathf.CeilToInt(prepareSeconds - Time.timeSinceLevelLoad));
            return;
        }

        // waveIntervalMin~waveIntervalMax(초) 무작위 간격마다 다음 웨이브로 전환(생존 적 전멸 대기 방식 폐지)
        if (Time.time >= nextSpawnTime)
        {
            wave++;
            SpawnWave();
            nextSpawnTime = Time.time + Random.Range(waveIntervalMin, waveIntervalMax);
        }

        // 보스 등장 시각(10분 간격)이 지났으면 소환 대기열에 추가하고, 소환 지점이 확보되는 대로 소환
        ScheduleBosses();
        SpawnPendingBosses();

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
        UIManager.instance.UpdateWaveText(wave, zombies.Count, pendingSpawns + pendingEliteSpawns);
    }

    // 현재 웨이브 번호를 기준으로 이번 웨이브에 생성할 좀비 수를 계산(예: 1,2,3,4웨이브 = 4,6,8,10마리)
    private void SpawnWave() {
        int spawnCount = baseWaveZombieCount + zombieCountIncreasePerWave * (wave - 1);

        // 강화 웨이브(eliteWaveInterval의 배수)에는 소환 마릿수의 일부를 강화 적으로 바꾼다
        int eliteCount = 0;
        if (eliteWaveInterval > 0 && wave % eliteWaveInterval == 0 && GetEliteDatas().Length > 0)
        {
            eliteCount = Mathf.Clamp(Mathf.RoundToInt(spawnCount * eliteRatio), 1, spawnCount);
        }

        // 바로 소환하지 않고 대기열에 넣어 두면 SpawnPending이 동시 상한 안에서 소환한다
        pendingSpawns += spawnCount - eliteCount;
        pendingEliteSpawns += eliteCount;
    }

    // 강화가 아닌(일반) 좀비 데이터 목록(강화 데이터만 있으면 전체를 사용)
    private ZombieData[] GetNormalDatas() {
        if (normalDatas == null)
        {
            normalDatas = System.Array.FindAll(zombieDatas, d => !d.isElite);
            if (normalDatas.Length == 0)
            {
                normalDatas = zombieDatas;
            }
        }

        return normalDatas;
    }

    // 강화 좀비 데이터 목록(없으면 빈 배열)
    private ZombieData[] GetEliteDatas() {
        if (eliteDatas == null)
        {
            eliteDatas = System.Array.FindAll(zombieDatas, d => d.isElite);
        }

        return eliteDatas;
    }

    // 경과 시간이 보스 등장 시각(bossIntervalMinutes의 배수)에 도달했는지 확인해 대기열에 추가
    private void ScheduleBosses() {
        if (GameManager.instance == null || bossIntervalMinutes <= 0f)
        {
            return;
        }

        while (GameManager.instance.elapsedMinutes >= bossIntervalMinutes * (bossesScheduled + 1))
        {
            bossesScheduled++;
            pendingBossMinutes.Enqueue(Mathf.RoundToInt(bossIntervalMinutes * bossesScheduled));
        }
    }

    // 대기 중인 보스를 소환(보스는 동시 상한에 막히지 않는다)
    private void SpawnPendingBosses() {
        while (pendingBossMinutes.Count > 0)
        {
            if (Time.time < spawnRetryTime)
            {
                return;
            }

            Transform spawnPoint;
            Vector3 spawnPosition;
            if (!TryGetSpawnPosition(out spawnPoint, out spawnPosition))
            {
                OnSpawnFailed();
                return;
            }

            spawnFailureStartTime = -1f;
            spawnFailureLogged = false;

            int minute = pendingBossMinutes.Dequeue();
            bool isGoalBoss = GameManager.instance != null && minute == Mathf.RoundToInt(GameManager.instance.primaryGoalMinutes);
            ZombieData data = minute < GameManager.instance.primaryGoalMinutes ? bossData : finalBossData;
            if (data == null)
            {
                // 보스 데이터가 연결되지 않았으면 일반 좀비로 대체하지 않고 건너뜀
                Debug.LogWarning("[ZombieSpawner] " + minute + "분 보스 데이터(bossData/finalBossData)가 연결되지 않아 보스를 소환하지 않습니다.");
                continue;
            }

            CreateZombie(spawnPoint, spawnPosition, data, isGoalBoss);
        }
    }

    // 동시 상한(maxConcurrentZombies)에 여유가 있는 만큼 대기 중인 좀비를 소환
    private void SpawnPending() {
        while ((pendingSpawns > 0 || pendingEliteSpawns > 0) && spawnedZombies.Count < maxConcurrentZombies)
        {
            // 후보가 모두 실패한 직후에는 spawnRetryDelay 동안 기다렸다가 다시 시도
            if (Time.time < spawnRetryTime)
            {
                return;
            }

            Transform spawnPoint;
            Vector3 spawnPosition;
            if (!TryGetSpawnPosition(out spawnPoint, out spawnPosition))
            {
                OnSpawnFailed();
                return;
            }

            spawnFailureStartTime = -1f;
            spawnFailureLogged = false;
            // 강화 적이 대기 중이면 먼저 소환(대기열이 길어도 강화 웨이브의 강화 적이 뒤로 밀리지 않게 함)
            if (pendingEliteSpawns > 0)
            {
                ZombieData[] elites = GetEliteDatas();
                CreateZombie(spawnPoint, spawnPosition, elites[Random.Range(0, elites.Length)]);
                pendingEliteSpawns--;
            }
            else
            {
                ZombieData[] normals = GetNormalDatas();
                CreateZombie(spawnPoint, spawnPosition, normals[Random.Range(0, normals.Length)]);
                pendingSpawns--;
            }
        }
    }

    // 스폰 지점 후보를 뽑아 사용할 수 있는 지점을 찾는다(NavMesh 위, 플레이어에서 충분히 멀고, 플레이어까지 경로가 완전)
    private bool TryGetSpawnPosition(out Transform spawnPoint, out Vector3 spawnPosition) {
        spawnPoint = null;
        spawnPosition = Vector3.zero;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < spawnCandidateTries; i++)
        {
            int index = Random.Range(0, spawnPoints.Length);
            if (IsSpawnPointUsable(index))
            {
                spawnPoint = spawnPoints[index];
                spawnPosition = spawnPointNavPosition[index];
                return true;
            }
        }

        return false;
    }

    // 스폰 지점 하나가 지금 사용 가능한지 검사(결과는 spawnPointCheckInterval 동안 재사용)
    private bool IsSpawnPointUsable(int index) {
        if (spawnPointCheckedAt == null || spawnPointCheckedAt.Length != spawnPoints.Length)
        {
            spawnPointCheckedAt = new float[spawnPoints.Length];
            spawnPointUsable = new bool[spawnPoints.Length];
            spawnPointNavPosition = new Vector3[spawnPoints.Length];
            for (int i = 0; i < spawnPointCheckedAt.Length; i++)
            {
                spawnPointCheckedAt[i] = -100f;
            }
        }

        if (Time.time < spawnPointCheckedAt[index] + spawnPointCheckInterval)
        {
            return spawnPointUsable[index];
        }

        spawnPointCheckedAt[index] = Time.time;
        spawnPointUsable[index] = false;

        // 1) NavMesh 위에 배치할 수 있어야 한다
        NavMeshHit spawnHit;
        if (!NavMesh.SamplePosition(spawnPoints[index].position, out spawnHit, spawnPointSampleDistance, NavMesh.AllAreas))
        {
            return false;
        }
        spawnPointNavPosition[index] = spawnHit.position;

        // 플레이어가 없거나 이미 사망했으면 거리·경로 검사는 생략
        if (cachedPlayerHealth == null)
        {
            cachedPlayerHealth = FindObjectOfType<PlayerHealth>();
        }
        if (cachedPlayerHealth == null || cachedPlayerHealth.dead)
        {
            spawnPointUsable[index] = true;
            return true;
        }

        // 2) 플레이어에서 최소 거리 이상(수평 기준) 떨어져 있어야 한다
        Vector3 toPlayer = cachedPlayerHealth.transform.position - spawnHit.position;
        toPlayer.y = 0f;
        if (toPlayer.magnitude < minSpawnDistanceFromPlayer)
        {
            return false;
        }

        // 3) 플레이어까지 경로가 완전해야 한다(플레이어 위치가 NavMesh에서 멀면 경로 검사는 생략해 스폰이 멈추지 않게 함)
        NavMeshHit playerHit;
        if (NavMesh.SamplePosition(cachedPlayerHealth.transform.position, out playerHit, spawnPointSampleDistance * 1.5f, NavMesh.AllAreas))
        {
            NavMeshPath path = new NavMeshPath();
            if (!NavMesh.CalculatePath(spawnHit.position, playerHit.position, NavMesh.AllAreas, path)
                || path.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }
        }

        spawnPointUsable[index] = true;
        return true;
    }

    // 사용 가능한 스폰 지점이 없을 때: 잠시 후 재시도하고, 오래 이어지면 오류로 기록
    private void OnSpawnFailed() {
        spawnRetryTime = Time.time + spawnRetryDelay;

        if (spawnFailureStartTime < 0f)
        {
            spawnFailureStartTime = Time.time;
        }
        else if (!spawnFailureLogged && Time.time - spawnFailureStartTime >= spawnFailureLimit)
        {
            spawnFailureLogged = true;
            string message = spawnFailureLimit + "초 동안 사용 가능한 스폰 지점을 찾지 못했습니다. 소환 대기: " + (pendingSpawns + pendingEliteSpawns);
            Debug.LogError("[ZombieSpawner] " + message);

            // 기획서 10장: 재배치·스폰이 모두 불가능하면 진행 오류 UI를 표시하고 재시작을 제공
            if (GameManager.instance != null)
            {
                GameManager.instance.EnterErrorState("NO SPAWN POINT AVAILABLE\nPLEASE RESTART");
            }
        }
    }

    // 좀비가 플레이어까지 유효한 경로를 얻지 못하고 있을 때: 사용 가능한 스폰 지점으로 옮긴다(체력·생존 집계 유지)
    private void RelocateZombie(Zombie zombie) {
        if (zombie == null || zombie.dead)
        {
            return;
        }

        Transform spawnPoint;
        Vector3 spawnPosition;
        if (TryGetSpawnPosition(out spawnPoint, out spawnPosition))
        {
            zombie.Relocate(spawnPosition, spawnPoint.rotation);
            spawnFailureStartTime = -1f;
            spawnFailureLogged = false;
        }
        else
        {
            if (Time.time - lastRelocateFailureLogTime >= 5f)
            {
                lastRelocateFailureLogTime = Time.time;
                Debug.LogWarning("[ZombieSpawner] 경로가 막힌 좀비를 옮길 스폰 지점이 없습니다: " + zombie.name);
            }

            // 스폰과 같은 조건이므로 연속 실패 시간을 함께 세어, 오래 이어지면 오류 상태로 전환
            OnSpawnFailed();
        }
    }

    // 좀비를 생성하고 생성한 좀비에게 추적할 대상을 할당
    // dataOverride가 있으면(보스) 그 데이터를 쓰고, 없으면 일반 좀비 데이터를 무작위로 고른다
    // isGoalBoss가 참인 보스를 처치하면 1차 목표를 달성한다
    private void CreateZombie(Transform spawnPoint, Vector3 spawnPosition, ZombieData dataOverride = null, bool isGoalBoss = false) {
        // 사용할 좀비 데이터를 결정
        ZombieData zombieData = dataOverride != null ? dataOverride : zombieDatas[Random.Range(0, zombieDatas.Length)];
        
        // 프리팹으로 좀비 생성(검증을 통과한 스폰 지점의 NavMesh 위 위치)
        Zombie zombie = Instantiate(zombiePrefab, spawnPosition, spawnPoint.rotation);
        
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
        int score = zombieData.score;
        zombie.onDeath += () => GameManager.instance.AddScore(score);
        zombie.onDeath += () => DropLoot(zombie.transform.position);
        zombie.onDeath += HealPlayerIfAllCleared;

        // 1차 목표 보스(30분 보스)를 처치하면 목표 달성 처리(무한 모드 진입)
        if (isGoalBoss)
        {
            zombie.onDeath += () => GameManager.instance.ReachPrimaryGoal();
        }

        // 경로가 막혀 5초 이상 플레이어에게 접근하지 못하면 다른 스폰 지점으로 옮김
        zombie.onPathBlocked += RelocateZombie;
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