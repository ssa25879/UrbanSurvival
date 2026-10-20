using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 모바일에서 기존 HUD 캔버스의 모서리 기준 패널(점수·웨이브·미니맵·탄약 등)이 노치·펀치홀·둥근 모서리에 가리지 않도록
// 안전 영역 컨테이너(SafeAreaFitter) 안으로 옮긴다(2026-10-20). 씬 파일은 수정하지 않고 런타임에 부모만 바꾼다.
// 앵커·위치 값은 그대로라 안전 영역이 화면 전체인 기기에서는 모양이 달라지지 않는다.
// 화면 전체를 덮는 창(게임오버·일시정지·설정 등)과 월드 좌표를 따라가는 표시(재장전 알림)는 옮기지 않는다
public static class MobileHudSafeArea {
    private const string ContainerName = "Mobile HUD Safe Area";
    private const string BossPanelName = "Boss Health UI"; // 전체 화면 크기 루트지만 내용이 위쪽 가운데에 있어 함께 옮긴다

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap() {
        SceneManager.sceneLoaded += (scene, mode) => Apply();
        Apply();
    }

    private static void Apply() {
        if (!MobilePlatform.IsMobile)
        {
            return;
        }

        WeaponHUD hud = Object.FindFirstObjectByType<WeaponHUD>(FindObjectsInactive.Include);
        if (hud == null)
        {
            return;
        }

        Canvas canvas = hud.GetComponentInParent<Canvas>(true);
        if (canvas == null)
        {
            return;
        }

        Transform root = canvas.rootCanvas.transform;
        if (root.Find(ContainerName) != null)
        {
            return;
        }

        var toMove = new List<Transform>();
        int firstIndex = int.MaxValue;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (ShouldMove(child))
            {
                toMove.Add(child);
                firstIndex = Mathf.Min(firstIndex, i);
            }
        }

        if (toMove.Count == 0)
        {
            return;
        }

        RectTransform container = MobileUIFactory.NewRect(ContainerName, root);
        MobileUIFactory.Stretch(container);
        container.gameObject.AddComponent<SafeAreaFitter>();

        for (int i = 0; i < toMove.Count; i++)
        {
            toMove[i].SetParent(container, false);
        }

        // 다른 전체 화면 창과의 앞뒤 순서를 유지하도록 옮긴 첫 요소가 있던 자리에 둔다
        container.SetSiblingIndex(firstIndex);
    }

    // 한 점에 고정된 앵커(모서리·가장자리 기준 패널)와 보스 체력 패널만 옮긴다
    private static bool ShouldMove(Transform child) {
        RectTransform rect = child as RectTransform;
        if (rect == null)
        {
            return false;
        }

        return rect.anchorMin == rect.anchorMax || child.name == BossPanelName;
    }
}
