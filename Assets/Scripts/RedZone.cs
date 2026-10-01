using UnityEngine;

// 보스 원거리 패턴이 바닥에 만드는 위험 지역(레드존)
// 경고 시간 동안 바깥 링은 고정으로 위험 범위를 보여 주고, 안쪽 채움이 중심에서부터 차오른다.
// 채움이 가득 차는 순간 범위 안의 플레이어에게 한 번 피해를 주고 잠깐 번쩍인 뒤 사라진다(2026-10-01 추가)
public class RedZone : MonoBehaviour {
    public Transform ring; // 위험 범위 바깥 링(지름 = 범위 x 2로 스케일)
    public Transform fill; // 차오르는 안쪽 채움(0 -> 지름)
    public Renderer ringRenderer; // 링 렌더러(색 변경용)
    public Renderer fillRenderer; // 채움 렌더러(색 변경용)
    public float height = 0.16f; // 바닥 위 높이(도로 연석 12~13cm보다 위)
    public float flashSeconds = 0.25f; // 폭발 후 번쩍이는 시간

    public Color warnColor = new Color(1f, 0.12f, 0.1f, 0.55f);
    public Color flashColor = new Color(1f, 0.85f, 0.6f, 0.9f);

    private float radius;
    private float warnSeconds;
    private float damage;
    private float percentOfMaxHealth; // 0보다 크면 플레이어 최대(전체) 체력 비율로 피해
    private float startTime;
    private bool exploded;
    private float explodeTime;
    private PlayerHealth player;
    private MaterialPropertyBlock block;

    // delaySeconds만큼 뒤에 경고가 시작되고(그 전에는 보이지 않음), warnSeconds 뒤에 폭발한다
    public static RedZone Spawn(RedZone prefab, Vector3 center, float radius, float warnSeconds, float damage, float delaySeconds, float percentOfMaxHealth = 0f) {
        RedZone zone = Instantiate(prefab, new Vector3(center.x, prefab.height, center.z), Quaternion.identity);
        zone.radius = radius;
        zone.warnSeconds = Mathf.Max(0.1f, warnSeconds);
        zone.damage = damage;
        zone.percentOfMaxHealth = percentOfMaxHealth;
        zone.startTime = Time.time + delaySeconds;
        zone.Apply(0f, zone.warnColor);
        return zone;
    }

    private void Awake() {
        block = new MaterialPropertyBlock();
        player = FindFirstObjectByType<PlayerHealth>();
        SetVisible(false);
    }

    private void Update() {
        if (Time.time < startTime)
        {
            return;
        }

        if (!exploded)
        {
            SetVisible(true);
            float t = Mathf.Clamp01((Time.time - startTime) / warnSeconds);
            // 거의 다 찰수록 진해져 임박함을 알린다
            Color c = warnColor;
            c.a = Mathf.Lerp(warnColor.a * 0.7f, warnColor.a * 1.3f, t);
            Apply(t, c);

            if (t >= 1f)
            {
                Explode();
            }
            return;
        }

        float flash = (Time.time - explodeTime) / flashSeconds;
        if (flash >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        Color f = flashColor;
        f.a = flashColor.a * (1f - flash);
        Apply(1f + flash * 0.08f, f);
    }

    private void Explode() {
        exploded = true;
        explodeTime = Time.time;

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerHealth>();
        }

        if (player != null && !player.dead)
        {
            Vector3 offset = player.transform.position - transform.position;
            offset.y = 0f;
            if (offset.magnitude <= radius)
            {
                float dealt = damage;
                if (percentOfMaxHealth > 0f)
                {
                    // 체력 비율 피해: 플레이어 최대(전체) 체력의 일정 비율(즉사 규칙 없음, 2026-10-01 제거)
                    dealt = player.startingHealth * percentOfMaxHealth;
                }

                player.OnDamage(dealt, player.transform.position, Vector3.up);
            }
        }
    }

    // 링은 항상 최대 범위, 채움은 fillRatio만큼
    private void Apply(float fillRatio, Color color) {
        float diameter = radius * 2f;
        if (ring != null)
        {
            ring.localScale = new Vector3(diameter, diameter, 1f);
        }
        if (fill != null)
        {
            float d = diameter * Mathf.Max(0.001f, fillRatio);
            fill.localScale = new Vector3(d, d, 1f);
        }

        SetColor(ringRenderer, color);
        SetColor(fillRenderer, color);
    }

    private void SetColor(Renderer target, Color color) {
        if (target == null || block == null)
        {
            return;
        }

        target.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);
        target.SetPropertyBlock(block);
    }

    private void SetVisible(bool visible) {
        if (ringRenderer != null) ringRenderer.enabled = visible;
        if (fillRenderer != null) fillRenderer.enabled = visible;
    }
}
