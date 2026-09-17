using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// 좀비 게임 오브젝트를 주기적으로 생성
public class ZombieSpawner : MonoBehaviour {
    public Zombie zombiePrefab; // 생성할 좀비 원본 프리팹

    public ZombieData[] zombieDatas; // 사용할 좀비 셋업 데이터들
    public Transform[] spawnPoints; // 좀비 AI를 소환할 위치들

    private List<Zombie> zombies = new List<Zombie>(); // 생성된 좀비들을 담는 리스트
    private int wave; // 현재 웨이브

    private readonly int zombieScore = 100;
    private readonly float despawnTime = 10.0f;

    private void Update() {
        // 게임 오버 상태일때는 생성하지 않음
        if (GameManager.instance != null && GameManager.instance.isGameover)
        {
            return;
        }

        // 좀비를 모두 물리친 경우 다음 스폰 실행
        if (zombies.Count <= 0)
        {
            SpawnWave();
        }

        // UI 갱신
        UpdateUI();
    }

    // 웨이브 정보를 UI로 표시
    private void UpdateUI() {
        // 현재 웨이브와 남은 적 수 표시
        UIManager.instance.UpdateWaveText(wave, zombies.Count);
    }

    // 현재 웨이브에 맞춰 좀비들을 생성
    private void SpawnWave() {
        // 웨이브 카운터 증가
        wave++;
        
        // 좀비 스폰량을 웨이브 * 1.5 만큼 소환
        int spawnCount = Mathf.RoundToInt(wave * 1.5f);
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
        
        // 생성한 좀비에 zombieData 할당하고 리스트에 추가
        zombie.Setup(zombieData);
        zombies.Add(zombie);
        
        // onDeath 이벤트에 메서드 등록 - 리스트에서 제거, 화면에서 제거, 점수 증가
        zombie.onDeath += () => zombies.Remove(zombie);
        zombie.onDeath += () => Destroy(zombie.gameObject, despawnTime);
        zombie.onDeath += () => GameManager.instance.AddScore(zombieScore);
    }
}