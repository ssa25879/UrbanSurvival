using GameSettingsKit;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 설정 창(GameSettingsKit)에 모바일 조준 모드 토글을 붙이는 연결 계층. 모듈은 이 클래스를 모른다
// VSync 행을 복제해 "TWIN-STICK AIM" 행을 만들고(씬 파일을 수정하지 않음), 모바일에서는 PC 전용 행(전체 화면·해상도·VSync)을 숨긴다
public static class MobileAimModeSettingsBinder {
    private const string RowName = "Mobile Aim Mode Row";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap() {
        SceneManager.sceneLoaded += (scene, mode) => BindAll();
        BindAll();
    }

    private static void BindAll() {
        if (!MobilePlatform.IsMobile)
        {
            return;
        }

        SettingsPanel[] panels = Object.FindObjectsByType<SettingsPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SettingsPanel panel in panels)
        {
            Bind(panel);
        }
    }

    private static void Bind(SettingsPanel panel) {
        if (panel.vSyncToggle == null)
        {
            return;
        }

        Transform vSyncRow = FindRow(panel.vSyncToggle.transform);
        if (vSyncRow == null || vSyncRow.parent.Find(RowName) != null)
        {
            return;
        }

        // VSync 행을 복제해 조준 모드 행으로 사용한다(원본을 숨기기 전에 복제해야 복제본이 켜진 상태로 만들어진다)
        GameObject clone = Object.Instantiate(vSyncRow.gameObject, vSyncRow.parent);
        clone.name = RowName;
        clone.SetActive(true);
        clone.transform.SetSiblingIndex(vSyncRow.GetSiblingIndex() + 1);

        // 모바일에서 의미 없는 PC 전용 행 숨김
        HideRow(panel.fullscreenToggle != null ? panel.fullscreenToggle.transform : null);
        HideRow(panel.resolutionDropdown != null ? panel.resolutionDropdown.transform : null);
        vSyncRow.gameObject.SetActive(false);

        Text label = clone.GetComponentInChildren<Text>(true);
        if (label != null)
        {
            label.text = "TWIN-STICK AIM";
        }

        Toggle toggle = clone.GetComponentInChildren<Toggle>(true);
        if (toggle == null)
        {
            Object.Destroy(clone);
            vSyncRow.gameObject.SetActive(true);
            return;
        }

        toggle.onValueChanged.RemoveAllListeners();
        toggle.SetIsOnWithoutNotify(MobileAimSettings.Mode == MobileAimMode.TwinStick);
        toggle.onValueChanged.AddListener(isOn => MobileAimSettings.Mode = isOn ? MobileAimMode.TwinStick : MobileAimMode.AutoAim);
        panel.onOpened.AddListener(() => toggle.SetIsOnWithoutNotify(MobileAimSettings.Mode == MobileAimMode.TwinStick));
    }

    // 컨트롤 → 행(이름이 "... Row"로 끝나는 조상)
    private static Transform FindRow(Transform control) {
        Transform current = control;
        while (current != null && !current.name.EndsWith(" Row"))
        {
            current = current.parent;
        }
        return current;
    }

    private static void HideRow(Transform control) {
        if (control == null)
        {
            return;
        }

        Transform row = FindRow(control);
        if (row != null)
        {
            row.gameObject.SetActive(false);
        }
    }
}
