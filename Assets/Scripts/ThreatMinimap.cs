using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 우측 상단 HUD: 플레이어를 미니맵 중앙에 고정하고, 탐지 반경 안의 좀비를 상대 위치(월드 북쪽 고정)로 표시
public class ThreatMinimap : MonoBehaviour {
    public RectTransform enemyMarkerLayer; // 좀비 마커를 담을 부모
    public Image enemyMarkerPrefab; // 좀비 마커로 사용할 아이콘(원형 스프라이트), 비활성 상태로 유지되는 템플릿

    public float detectionRadius = 35f; // 미니맵에 표시할 실제 탐지 반경(m, HUD 준비 문서 9장 초기 제안값)
    public float minimapPixelRadius = 110f; // 탐지 반경에 대응하는 미니맵 픽셀 반지름(HUD 준비 문서 9장 초기 제안값: 220px 미니맵의 절반)
    public float refreshInterval = 0.15f; // 좀비 목록 재탐색 주기(매 프레임 전체 탐색 방지)

    private Transform playerTransform;
    private readonly List<Image> markerPool = new List<Image>();
    private float lastRefreshTime;

    private void Start() {
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerTransform = playerHealth.transform;
        }
    }

    private void Update() {
        if (playerTransform == null)
        {
            PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth == null)
            {
                return;
            }
            playerTransform = playerHealth.transform;
        }

        if (Time.time >= lastRefreshTime + refreshInterval)
        {
            lastRefreshTime = Time.time;
            RefreshMarkers();
        }
    }

    private void RefreshMarkers() {
        Zombie[] zombies = FindObjectsByType<Zombie>(FindObjectsSortMode.None);

        int usedMarkers = 0;
        for (int i = 0; i < zombies.Length; i++)
        {
            if (zombies[i].dead)
            {
                continue;
            }

            Vector3 offset = zombies[i].transform.position - playerTransform.position;
            Vector2 flatOffset = new Vector2(offset.x, offset.z);
            float distance = flatOffset.magnitude;

            if (distance > detectionRadius)
            {
                continue;
            }

            // 월드 XZ를 미니맵 로컬 좌표(위쪽 = 월드 북쪽)로 변환, 반경 안쪽으로 클램프
            Vector2 mapPos = flatOffset / detectionRadius * minimapPixelRadius;
            if (mapPos.magnitude > minimapPixelRadius)
            {
                mapPos = mapPos.normalized * minimapPixelRadius;
            }

            Image marker = GetMarker(usedMarkers);
            marker.rectTransform.anchoredPosition = mapPos;
            marker.gameObject.SetActive(true);
            usedMarkers++;
        }

        // 이번 갱신에서 쓰지 않은 나머지 마커는 비활성화(다음 갱신에서 재사용)
        for (int i = usedMarkers; i < markerPool.Count; i++)
        {
            markerPool[i].gameObject.SetActive(false);
        }
    }

    private Image GetMarker(int index) {
        if (index < markerPool.Count)
        {
            return markerPool[index];
        }

        Image marker = Instantiate(enemyMarkerPrefab, enemyMarkerLayer);
        markerPool.Add(marker);
        return marker;
    }
}
