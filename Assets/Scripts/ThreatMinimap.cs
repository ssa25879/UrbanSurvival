using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 우측 상단 HUD: 플레이어를 미니맵 중앙에 고정하고, 탐지 반경 안의 좀비를 상대 위치(월드 북쪽 고정)로 표시
public class ThreatMinimap : MonoBehaviour {
    public RectTransform enemyMarkerLayer; // 좀비 마커를 담을 부모
    public Image enemyMarkerPrefab; // 좀비 마커로 사용할 아이콘(원형 스프라이트), 비활성 상태로 유지되는 템플릿
    public Sprite offscreenArrowSprite; // 미니맵 범위 밖 위협을 표시할 방향 화살표 스프라이트(북쪽 기준으로 위를 향하는 아이콘)

    public float detectionRadius = 35f; // 미니맵에 점으로 표시할 실제 탐지 반경(m, HUD 준비 문서 9장 초기 제안값)
    public float awarenessRadius = 70f; // 이 범위 안(탐지 반경 밖 포함)의 적은 가장자리 화살표로 존재만 알림
    public float minimapPixelRadius = 110f; // 탐지 반경에 대응하는 미니맵 픽셀 반지름(HUD 준비 문서 9장 초기 제안값: 220px 미니맵의 절반)
    public float edgeArrowInset = 12f; // 화면 밖 화살표를 미니맵 테두리에서 안쪽으로 띄우는 거리(px)
    public float refreshInterval = 0.15f; // 좀비 목록 재탐색 주기(매 프레임 전체 탐색 방지)

    [Header("강화 개체 구분(2026-09-24 추가)")]
    public Color eliteColor = new Color(1f, 0.55f, 0f, 1f); // 강화 개체 마커 색(주황)
    public float eliteScaleMultiplier = 1.6f; // 강화 개체 마커 확대 배율

    private Transform playerTransform;
    private readonly List<Image> markerPool = new List<Image>();
    private float lastRefreshTime;

    private Sprite dotSprite; // 탐지 반경 안(점) 표시에 쓸 기본 스프라이트
    private Color normalColor; // 일반 개체 마커 색(프리팹에 이미 지정된 색 그대로 사용)
    private Vector2 normalDotSize; // 일반 개체 점 크기(프리팹 기본값)
    private Vector2 normalArrowSize; // 일반 개체 화살표 크기(점 크기와 동일하게 시작)

    private void Start() {
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerTransform = playerHealth.transform;
        }

        // 프리팹에 이미 설정된 값을 일반 개체 기본값으로 사용(하드코딩 대신 기존 설정 존중)
        dotSprite = enemyMarkerPrefab.sprite;
        normalColor = enemyMarkerPrefab.color;
        normalDotSize = enemyMarkerPrefab.rectTransform.sizeDelta;
        normalArrowSize = normalDotSize;
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

            // 인지 범위(awarenessRadius) 밖은 화살표로도 표시하지 않음
            if (distance > awarenessRadius)
            {
                continue;
            }

            bool isElite = zombies[i].zombieData != null && zombies[i].zombieData.isElite;
            Image marker = GetMarker(usedMarkers);
            RectTransform markerRect = marker.rectTransform;
            marker.color = isElite ? eliteColor : normalColor;

            if (distance <= detectionRadius)
            {
                // 탐지 반경 안: 기존처럼 상대 위치에 점으로 표시(월드 XZ -> 미니맵, 위쪽 = 월드 북쪽)
                Vector2 mapPos = flatOffset / detectionRadius * minimapPixelRadius;
                marker.sprite = dotSprite;
                markerRect.localRotation = Quaternion.identity;
                markerRect.anchoredPosition = mapPos;
                markerRect.sizeDelta = isElite ? normalDotSize * eliteScaleMultiplier : normalDotSize;
            }
            else
            {
                // 탐지 반경 밖(인지 범위 안): 미니맵 가장자리에 방향 화살표로 존재만 알림
                Vector2 dir = flatOffset.normalized;
                marker.sprite = offscreenArrowSprite != null ? offscreenArrowSprite : dotSprite;
                markerRect.localRotation = Quaternion.FromToRotation(Vector3.up, new Vector3(dir.x, dir.y, 0f));
                markerRect.anchoredPosition = dir * (minimapPixelRadius - edgeArrowInset);
                markerRect.sizeDelta = isElite ? normalArrowSize * eliteScaleMultiplier : normalArrowSize;
            }

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
