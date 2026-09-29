using UnityEngine;
using UnityEngine.UI;

// 보스 머리 위에 표시하는 작은 체력바(월드 스페이스, 카메라를 향함)
// 보스 프리팹을 수정하지 않도록 보스 셋업 시 코드로 만든다. 보스가 파괴되면 함께 사라진다
public class BossHeadBar : MonoBehaviour {
    private static readonly Color backgroundColor = new Color(0.07f, 0.08f, 0.09f, 0.9f); // HUD 패널 톤
    private static readonly Color fillColor = new Color(0.9f, 0.28f, 0.3f, 1f); // 위험 빨강

    private const float BarWidth = 2.4f; // 월드 폭(m)
    private const float BarHeight = 0.3f; // 월드 높이(m)
    private const float HeightAboveBoss = 2.8f; // 보스 발밑 기준 바 높이(m)

    private Zombie target; // 체력을 표시할 보스
    private RectTransform fillRect; // 남은 체력 비율만큼 폭을 줄이는 채움 영역
    private Camera viewCamera; // 바를 정면으로 돌릴 카메라

    // 보스에 머리 위 체력바를 붙인다(보스와 별개 루트 오브젝트라 보스 크기 배율의 영향을 받지 않음)
    public static BossHeadBar Attach(Zombie boss) {
        GameObject barObject = new GameObject("Boss Head Bar", typeof(RectTransform), typeof(Canvas));
        BossHeadBar bar = barObject.AddComponent<BossHeadBar>();
        bar.Build(boss);
        return bar;
    }

    private void Build(Zombie boss) {
        target = boss;

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // 240 x 30 단위 RectTransform을 0.01 배율로 두면 월드에서 2.4 x 0.3 m가 된다
        RectTransform root = (RectTransform)transform;
        root.sizeDelta = new Vector2(BarWidth * 100f, BarHeight * 100f);
        root.localScale = Vector3.one * 0.01f;

        Image background = new GameObject("Background", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        background.transform.SetParent(root, false);
        background.color = backgroundColor;
        background.raycastTarget = false;
        Stretch((RectTransform)background.transform);

        Image fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        fill.transform.SetParent(root, false);
        fill.color = fillColor;
        fill.raycastTarget = false;
        fillRect = (RectTransform)fill.transform;
        Stretch(fillRect);
        // 바깥 테두리가 보이도록 안쪽으로 살짝 들여쓴다
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
    }

    private static void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void LateUpdate() {
        // 보스가 파괴되면 바도 제거, 사망하면 숨김
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        if (target.dead)
        {
            gameObject.SetActive(false);
            return;
        }

        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        transform.position = target.transform.position + Vector3.up * HeightAboveBoss;
        if (viewCamera != null)
        {
            transform.rotation = viewCamera.transform.rotation;
        }

        float ratio = target.startingHealth > 0f ? Mathf.Clamp01(target.health / target.startingHealth) : 0f;
        // 채움 영역의 오른쪽 끝을 비율만큼 왼쪽으로 당겨 체력이 줄어드는 것을 표시(들여쓰기 3 단위 유지)
        float innerWidth = ((RectTransform)transform).sizeDelta.x - 6f;
        fillRect.offsetMax = new Vector2(-3f - innerWidth * (1f - ratio), -3f);
    }
}
