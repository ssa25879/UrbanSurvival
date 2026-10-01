using UnityEngine;
using UnityEngine.UI;

// 미니맵 배경에 현재 지형(도로·블록·차량·바리케이드·건물)을 표시한다(2026-10-01 추가).
// 플레이어 위에서 내려다보는 직교 카메라를 작은 RenderTexture로 렌더해 RawImage에 보여 준다.
// 카메라 방향은 ThreatMinimap과 같은 기준(메인 카메라의 화면 위쪽)이라 적 점·화살표와 위치가 어긋나지 않는다.
// 표시 반경은 ThreatMinimap.detectionRadius와 같게 유지한다(적 점이 지형 위 같은 자리에 찍힘).
[RequireComponent(typeof(RawImage))]
public class MinimapTerrainView : MonoBehaviour {
    public ThreatMinimap minimap; // 표시 반경(detectionRadius)과 방향 기준(viewReference)을 가져올 미니맵
    public int textureSize = 256; // 렌더 텍스처 한 변(px)
    public float cameraHeight = 40f; // 플레이어 위 카메라 높이(m), 가장 높은 건물(약 10m)보다 높아야 함
    public LayerMask excludedLayers; // 지형 렌더에서 뺄 레이어(Player, UI, WeaponIcon 등)
    public Color backgroundColor = new Color(0.06f, 0.07f, 0.08f, 1f);

    private RawImage image;
    private RenderTexture renderTexture;
    private Camera terrainCamera;
    private Transform playerTransform;

    private void Awake() {
        image = GetComponent<RawImage>();
        renderTexture = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.Default);
        renderTexture.name = "MinimapTerrainRT";
        image.texture = renderTexture;

        GameObject cameraObject = new GameObject("Minimap Terrain Camera");
        terrainCamera = cameraObject.AddComponent<Camera>();
        terrainCamera.orthographic = true;
        terrainCamera.nearClipPlane = 0.3f;
        terrainCamera.farClipPlane = cameraHeight + 10f;
        terrainCamera.clearFlags = CameraClearFlags.SolidColor;
        terrainCamera.backgroundColor = backgroundColor;
        terrainCamera.cullingMask = ~excludedLayers.value;
        terrainCamera.targetTexture = renderTexture;
        terrainCamera.allowMSAA = false;
        terrainCamera.depth = -10f; // 메인 카메라보다 먼저 렌더

        // 후처리·그림자는 작은 지도에 필요 없어 끈다(비용 절감, 비네트 등이 지도에 겹치지 않게)
        var cameraData = cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = false;
        cameraData.renderShadows = false;
        cameraData.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None;

        // 씬에 오디오 리스너가 둘이 되지 않도록 카메라에는 따로 붙이지 않는다
    }

    private void OnDestroy() {
        if (terrainCamera != null)
        {
            Destroy(terrainCamera.gameObject);
        }
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }

    private void LateUpdate() {
        if (terrainCamera == null)
        {
            return;
        }

        if (playerTransform == null)
        {
            PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth == null)
            {
                return;
            }
            playerTransform = playerHealth.transform;
        }

        float radius = minimap != null ? minimap.detectionRadius : 35f;
        terrainCamera.orthographicSize = radius;

        // 메인 카메라의 오른쪽을 수평면에 투영하고 그 앞쪽을 미니맵 위쪽으로 쓴다(ThreatMinimap과 동일)
        Vector3 viewForward = Vector3.forward;
        Transform view = minimap != null && minimap.viewReference != null ? minimap.viewReference : (Camera.main != null ? Camera.main.transform : null);
        if (view != null)
        {
            Vector3 viewRight = Vector3.ProjectOnPlane(view.right, Vector3.up).normalized;
            viewForward = Vector3.Cross(viewRight, Vector3.up);
        }

        Vector3 position = playerTransform.position;
        terrainCamera.transform.position = new Vector3(position.x, position.y + cameraHeight, position.z);
        terrainCamera.transform.rotation = Quaternion.LookRotation(Vector3.down, viewForward);
    }
}
