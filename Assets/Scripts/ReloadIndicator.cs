using UnityEngine;
using UnityEngine.UI;

// 플레이어 머리 위 재장전 알림: 탄창이 비었을 때 재장전 안내, 재장전 진행 바
// (탄창이 비면 자동 재장전하지 않는 규칙이라, 연사 중 사격이 갑자기 멈춘 이유를
//  우하단 무기 패널까지 시선을 옮기지 않고 캐릭터 주변에서 바로 알 수 있도록 함)
public class ReloadIndicator : MonoBehaviour {
    public RectTransform content; // 머리 위를 따라다니며 켜고 끌 표시 묶음(이 오브젝트는 항상 활성)
    public Text messageText; // 상태 문구
    public GameObject progressBar; // 재장전 진행 바 전체(재장전 중에만 표시)
    public Image progressFill; // Image.Type = Filled(Horizontal)

    public Vector3 worldOffset = new Vector3(0f, 1.1f, 0f); // 플레이어 발밑 기준 표시 높이(m)
    public Vector2 screenOffset = new Vector2(0f, 56f); // 위 위치에서 화면(캔버스) 기준으로 더 띄우는 거리. 위에서 내려다보는 시점이라 월드 높이만으로는 캐릭터 몸과 겹친다
    public float blinkSpeed = 8f; // 탄창이 비었을 때 깜빡임 속도

    public Color emptyColor = new Color(0.90f, 0.28f, 0.30f, 1f); // 빨강(HUD 위험색)
    public Color reloadingColor = new Color(0.95f, 0.95f, 0.93f, 1f); // 흰색

    private PlayerShooter playerShooter;
    private Camera mainCamera;

    private void Start() {
        playerShooter = FindFirstObjectByType<PlayerShooter>();
        mainCamera = Camera.main;
    }

    private void LateUpdate() {
        if (playerShooter == null)
        {
            playerShooter = FindFirstObjectByType<PlayerShooter>();
        }
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        Gun gun = playerShooter != null ? playerShooter.gun : null;
        bool hidden = gun == null || gun.gunData == null || mainCamera == null
            || (GameManager.instance != null && (GameManager.instance.isGameover || GameManager.instance.isPaused));

        string message = null;
        Color color = reloadingColor;
        bool showProgress = false;

        if (!hidden)
        {
            if (gun.state == Gun.State.Reloading)
            {
                message = "RELOADING";
                showProgress = true;
                progressFill.fillAmount = gun.reloadProgress;
            }
            else if (gun.state == Gun.State.Empty)
            {
                // 예비탄까지 없으면 재장전이 불가능하므로 항상 사용 가능한 권총(1번 슬롯)으로 교체를 안내
                message = gun.ammoRemain == 0 ? "NO AMMO  [1]" : "RELOAD  [R]";
                color = emptyColor;
                color.a = Mathf.Lerp(0.5f, 1f, (Mathf.Sin(Time.unscaledTime * blinkSpeed) + 1f) * 0.5f);
            }
        }

        bool visible = message != null;
        if (content.gameObject.activeSelf != visible)
        {
            content.gameObject.SetActive(visible);
        }
        if (!visible)
        {
            return;
        }

        messageText.text = message;
        messageText.color = color;
        if (progressBar.activeSelf != showProgress)
        {
            progressBar.SetActive(showProgress);
        }

        // 플레이어 머리 위 월드 위치를 화면 좌표로 변환해 따라다니게 함
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(playerShooter.transform.position + worldOffset);
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screenPoint, null, out localPoint))
        {
            content.anchoredPosition = localPoint + screenOffset;
        }
    }
}
