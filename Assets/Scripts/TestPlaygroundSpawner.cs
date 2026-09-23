#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// 테스트 플레이 전용: UrbanSurvival 씬에서 Play Mode 진입 시 플레이어 주변에
// 아이템 전종류(무기 픽업 3종 + 탄약 + 회복)와 이동하지 않는 적 3종을 배치한다.
// 에디터 전용(#if UNITY_EDITOR)이라 실제 빌드에는 포함되지 않는다.
public static class TestPlaygroundSpawner {
    private const string TargetSceneName = "UrbanSurvival";
    private const float NavMeshSampleDistance = 10f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SpawnPlayground() {
        if (SceneManager.GetActiveScene().name != TargetSceneName) return;

        PlayerHealth player = Object.FindObjectOfType<PlayerHealth>();
        if (player == null) return;

        Vector3 origin = player.transform.position;
        Vector3 forward = player.transform.forward;
        Vector3 right = player.transform.right;

        // 아이템 전종류를 플레이어 앞쪽에 부채꼴로 배치
        SpawnItem("Assets/Prefabs/RiflePickup.prefab", origin + forward * 3f + right * -4f);
        SpawnItem("Assets/Prefabs/SMGPickup.prefab", origin + forward * 3f + right * -2f);
        SpawnItem("Assets/Prefabs/ShotgunPickup.prefab", origin + forward * 3f);
        SpawnItem("Assets/Prefabs/AmmoPack.prefab", origin + forward * 3f + right * 2f);
        SpawnItem("Assets/Prefabs/HealthPack.prefab", origin + forward * 3f + right * 4f);

        // 이동하지 않는 적 3종(데이터셋별 외형·크기 확인용)을 더 앞쪽에 배치
        SpawnStationaryZombie(origin + forward * 8f + right * -3f, "Assets/ScriptableData/Zombie Default.asset");
        SpawnStationaryZombie(origin + forward * 8f, "Assets/ScriptableData/Zombie Fast.asset");
        SpawnStationaryZombie(origin + forward * 8f + right * 3f, "Assets/ScriptableData/Zombie Heavy.asset");
    }

    private static void SpawnItem(string prefabPath, Vector3 desiredPosition) {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) {
            Debug.LogWarning($"[TestPlaygroundSpawner] 프리팹을 찾지 못함: {prefabPath}");
            return;
        }

        Vector3 spawnPosition = SampleNavMesh(desiredPosition) + Vector3.up * 0.5f;
        Object.Instantiate(prefab, spawnPosition, Quaternion.identity);
    }

    private static void SpawnStationaryZombie(Vector3 desiredPosition, string zombieDataPath) {
        GameObject zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Zombie.prefab");
        ZombieData zombieData = AssetDatabase.LoadAssetAtPath<ZombieData>(zombieDataPath);
        if (zombiePrefab == null || zombieData == null) {
            Debug.LogWarning($"[TestPlaygroundSpawner] 좀비 프리팹 또는 데이터를 찾지 못함: {zombieDataPath}");
            return;
        }

        Vector3 spawnPosition = SampleNavMesh(desiredPosition);
        GameObject instance = Object.Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);

        Zombie zombie = instance.GetComponent<Zombie>();
        zombie.Setup(zombieData);

        // 플레이어를 인식해도 쫓아오지 않도록 이동속도를 고정(외형·명중 테스트용 정지 표적)
        NavMeshAgent agent = instance.GetComponent<NavMeshAgent>();
        if (agent != null) {
            agent.speed = 0f;
            agent.angularSpeed = 0f;
        }
    }

    private static Vector3 SampleNavMesh(Vector3 desiredPosition) {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(desiredPosition, out hit, NavMeshSampleDistance, NavMesh.AllAreas)) {
            return hit.position;
        }
        return desiredPosition;
    }
}
#endif
