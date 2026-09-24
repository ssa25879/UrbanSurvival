using System;
using UnityEngine;
using UnityEngine.UI;

// 현재 장착한 무기의 실제 3D 모델을 전용 카메라로 촬영해 HUD 아이콘으로 표시
// 외부 2D 아이콘 에셋 대신 게임 내 실제 무기 모델을 그대로 사용(아이콘 카메라 기법)
public class WeaponIconRig : MonoBehaviour {
    public PlayerShooter playerShooter;
    public Camera iconCamera; // WeaponIcon 레이어만 촬영하는 전용 카메라
    public Transform stage; // 무기 사본이 놓일 위치(아이콘 카메라 정면, 씬의 플레이어와는 무관한 고정 위치)
    public RawImage targetImage; // 촬영 결과(RenderTexture)를 표시할 UI 이미지

    private const string IconLayerName = "WeaponIcon";

    private GameObject[] iconInstances; // playerShooter.guns와 같은 순서로 대응
    private int activeIndex = -1;

    private void Start() {
        if (playerShooter == null || iconCamera == null || stage == null)
        {
            enabled = false;
            return;
        }

        int iconLayer = LayerMask.NameToLayer(IconLayerName);
        iconCamera.cullingMask = 1 << iconLayer;

        Gun[] guns = playerShooter.guns;
        iconInstances = new GameObject[guns.Length];

        for (int i = 0; i < guns.Length; i++)
        {
            if (guns[i] == null)
            {
                continue;
            }

            GameObject source = guns[i].gameObject;
            GameObject copy = Instantiate(source, stage);
            copy.name = source.name + "_Icon";
            // 원본과 동일한 로컬 자세(핸드 소켓 기준 보정값)를 stage 기준으로 그대로 적용
            // 크기는 localScale이 아니라 lossyScale(캐릭터 리그 누적 스케일 포함 실제 월드 크기)을 사용해야
            // 스케일 체인이 없는 stage 밑에서도 원본과 동일한 크기로 보인다
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = source.transform.localRotation;
            copy.transform.localScale = source.transform.lossyScale;

            // 사본은 발사 로직이 필요 없으므로 제거, 이펙트도 정지 상태로 고정
            Gun gunComp = copy.GetComponent<Gun>();
            if (gunComp != null)
            {
                Destroy(gunComp);
            }

            Transform muzzle = copy.transform.Find("MuzzleFlashEffect");
            if (muzzle != null) muzzle.gameObject.SetActive(false);
            Transform shell = copy.transform.Find("ShellEjectEffect");
            if (shell != null) shell.gameObject.SetActive(false);

            SetLayerRecursively(copy, iconLayer);
            copy.SetActive(false);

            iconInstances[i] = copy;
        }

        if (targetImage != null)
        {
            targetImage.texture = iconCamera.targetTexture;
        }
    }

    private void Update() {
        if (iconInstances == null || playerShooter.gun == null)
        {
            return;
        }

        int currentIndex = Array.IndexOf(playerShooter.guns, playerShooter.gun);
        if (currentIndex == activeIndex)
        {
            return;
        }

        if (activeIndex >= 0 && activeIndex < iconInstances.Length && iconInstances[activeIndex] != null)
        {
            iconInstances[activeIndex].SetActive(false);
        }

        if (currentIndex >= 0 && currentIndex < iconInstances.Length && iconInstances[currentIndex] != null)
        {
            iconInstances[currentIndex].SetActive(true);
        }

        activeIndex = currentIndex;
    }

    private static void SetLayerRecursively(GameObject obj, int layer) {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
