using UnityEngine;

// 습득 범위에 들어온 아이템이 플레이어 쪽으로 날아간 뒤, 도착하면 효과를 적용한다(2026-10-07 설계, 비행 0.3초 사용자 확정).
// 범위 진입 판정은 기존대로 PlayerHealth.OnTriggerEnter가 하고, 이 컴포넌트가 있으면 즉시 Use 대신 Begin을 호출한다.
// 이동은 Time.deltaTime 기준이라 일시정지(timeScale 0) 중에는 비행도 멈춘다.
[RequireComponent(typeof(Collider))]
public class ItemMagnet : MonoBehaviour {
    [Tooltip("범위에 들어온 뒤 도착까지 걸리는 시간(초)")]
    public float flightDuration = 0.3f;

    [Tooltip("도착 지점의 플레이어 발 기준 높이(m)")]
    public float targetHeight = 0.8f;

    private const string GlowPadName = "GlowPad"; // 무기 픽업의 바닥 글로우, 비행 시작 시 숨김

    private IItem item;
    private PlayerHealth collector;
    private Vector3 startPosition;
    private float elapsed;
    private bool flying;

    // 비행을 시작한다. 이미 날고 있으면 무시한다(중복 습득 방지)
    public void Begin(PlayerHealth player) {
        if (flying) return;

        item = GetComponent<IItem>();
        if (item == null) return;

        collector = player;
        startPosition = transform.position;
        elapsed = 0f;
        flying = true;

        // 트리거를 꺼서 비행 중 다시 진입 판정이 일어나지 않게 한다
        GetComponent<Collider>().enabled = false;

        Transform glowPad = transform.Find(GlowPadName);
        if (glowPad != null) glowPad.gameObject.SetActive(false);
    }

    private void Update() {
        if (!flying) return;

        // 비행 중 플레이어가 사망했거나 사라지면 중단한다(아이템은 그 자리에 남고 효과는 적용되지 않음)
        if (collector == null || collector.dead)
        {
            flying = false;
            enabled = false;
            return;
        }

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(flightDuration, 0.01f));
        // 점점 빨라지는 가속(ease-in). 목표는 매 프레임 현재 플레이어 위치라 움직이는 플레이어에게도 도착한다
        float eased = t * t;
        Vector3 target = collector.transform.position + Vector3.up * targetHeight;
        transform.position = Vector3.Lerp(startPosition, target, eased);

        if (t >= 1f)
        {
            flying = false;
            // 도착 시 효과 적용과 습득음 재생(Use 안에서 아이템이 파괴된다)
            collector.CollectItem(item);
        }
    }
}
